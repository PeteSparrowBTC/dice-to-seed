using System.Text.RegularExpressions;
using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The rule that one roll log never produces both a seed phrase and a backup key.
///
/// This was correct and untested until now: the hazard it protects against is asserted by
/// <see cref="BackupKeyTests"/>, while the protection itself was a handful of lines in a 1267-line
/// Razor component that nothing would have noticed the removal of. The clause in CLAUDE.md rule 9
/// reads "switching mode clears the roll log", and this is the file that makes that true rather
/// than intended.
///
/// The hazard, so this file stands on its own: <c>k</c> is SHA-256 of the roll log, and a 24-word
/// seed's BIP-39 entropy is that same hash byte for byte. A log used for both makes the backup key
/// derivable from the wallet it protects, and the shares stop protecting anything.
/// </summary>
public class ModeSwitchTests
{
    static readonly IReadOnlyList<DerivationMode> Modes = [DerivationMode.SeedPhrase, DerivationMode.BackupKey];

    const string FiftyRolls = "12345612345612345612345612345612345612345612345612";

    /// <summary>
    /// The invariant, checked over every combination rather than the interesting one: if the mode
    /// came out different from the one that went in, the log that came out is empty. There is no
    /// path through this that changes mode and keeps rolls.
    /// </summary>
    [Fact]
    public void A_mode_that_actually_changes_never_keeps_the_log()
    {
        var logs = new[] { FiftyRolls, "1", "666666", string.Empty };

        var kept = from current in Modes
                   from requested in Modes
                   from log in logs
                   let result = ModeSwitch.Apply(current, requested, log)
                   where result.Mode != current && result.Rolls.Length > 0
                   select $"{current} to {requested} kept {result.Rolls.Length} rolls";

        Assert.Empty(kept);
    }

    /// <summary>
    /// And the other half, or the app would be unusable: asking for the mode you are already in is
    /// not a change and must not destroy the log. Pressing the button you are already on is the
    /// most likely accidental press on the page.
    /// </summary>
    [Theory]
    [InlineData(DerivationMode.SeedPhrase)]
    [InlineData(DerivationMode.BackupKey)]
    public void Asking_for_the_mode_already_in_effect_keeps_the_log(DerivationMode mode)
    {
        var (resulting, rolls) = ModeSwitch.Apply(mode, mode, FiftyRolls);

        Assert.Equal(mode, resulting);
        Assert.Equal(FiftyRolls, rolls);
    }

    /// <summary>
    /// A real switch lands in the requested mode with nothing recorded, which is the state a fresh
    /// session for the other purpose has to start from.
    /// </summary>
    [Theory]
    [InlineData(DerivationMode.SeedPhrase, DerivationMode.BackupKey)]
    [InlineData(DerivationMode.BackupKey, DerivationMode.SeedPhrase)]
    public void A_real_switch_lands_in_the_requested_mode_with_an_empty_log(
        DerivationMode current, DerivationMode requested)
    {
        var result = ModeSwitch.Apply(current, requested, FiftyRolls);

        Assert.Equal(requested, result.Mode);
        Assert.Equal(string.Empty, result.Rolls);
    }

    /// <summary>
    /// The confirmation is raised exactly when there is something to lose, and not otherwise. An
    /// app that asks when nothing is recorded trains the user to dismiss the question, which is
    /// worse than not asking, because the one time it matters it gets dismissed too.
    /// </summary>
    [Theory]
    [InlineData(DerivationMode.SeedPhrase, DerivationMode.BackupKey, 50, true)]
    [InlineData(DerivationMode.BackupKey, DerivationMode.SeedPhrase, 1, true)]
    [InlineData(DerivationMode.SeedPhrase, DerivationMode.BackupKey, 0, false)]
    [InlineData(DerivationMode.SeedPhrase, DerivationMode.SeedPhrase, 50, false)]
    [InlineData(DerivationMode.BackupKey, DerivationMode.BackupKey, 99, false)]
    public void Confirmation_is_asked_for_only_when_a_log_would_be_lost(
        DerivationMode current, DerivationMode requested, int recordedRolls, bool expected) =>
        Assert.Equal(expected, ModeSwitch.RequiresConfirmation(current, requested, recordedRolls));

    /// <summary>
    /// The two halves have to agree. Whenever a confirmation is required, applying the switch must
    /// be the thing that clears the log, otherwise the dialog is warning about something that does
    /// not happen.
    /// </summary>
    [Fact]
    public void Whenever_confirmation_is_required_applying_the_switch_clears_the_log()
    {
        var pairs = from current in Modes
                    from requested in Modes
                    where ModeSwitch.RequiresConfirmation(current, requested, FiftyRolls.Length)
                    select ModeSwitch.Apply(current, requested, FiftyRolls);

        var results = pairs.ToList();

        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.Equal(string.Empty, result.Rolls));
    }

    /// <summary>
    /// The page must route through this rather than reimplement it. A weak check by construction:
    /// it proves the call is present, not that no other path exists. It is here because the failure
    /// this file was written for is somebody deleting the clearing, and deleting the call is how
    /// that would now look.
    ///
    /// Matched as an assignment rather than as a mention, and that distinction was found the hard
    /// way: the first version asserted the page merely contained "ModeSwitch.Apply", which passed
    /// with the call deleted, because the XML doc comment one line above says
    /// <c>see cref="ModeSwitch.Apply"</c>. A guard that matches its own documentation is not a guard.
    /// It is the third time that shape of mistake has appeared in this repository, after the favicon
    /// namespace and the loading ring, so the pattern is worth naming: assert the thing that does the
    /// work, not the words describing it.
    ///
    /// The assignment form is also the property worth pinning. Both fields have to come out of one
    /// call, or the mode can end up changed while a log from the other purpose is still recorded.
    /// </summary>
    [Fact]
    public void The_page_applies_the_switch_through_this_rule()
    {
        var page = File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "DiceToSeed.Web", "Pages", "Derive.razor"));

        Assert.Matches(@"\(\s*mode\s*,\s*rolls\s*\)\s*=\s*ModeSwitch\.Apply\(", page);
        Assert.Matches(@"ModeSwitch\.RequiresConfirmation\(", page);

        // And it no longer carries its own copy of the enum, which is what let the rule live in a
        // view in the first place.
        Assert.False(Regex.IsMatch(page, @"\benum\s+DerivationMode\b"),
            "Derive.razor declares its own DerivationMode again, so the page and Core can now disagree about what a mode is.");
    }

    static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("DiceToSeed.slnx").Any())
            directory = directory.Parent;

        return directory ?? throw new InvalidOperationException(
            $"Could not find DiceToSeed.slnx above {AppContext.BaseDirectory}.");
    }
}
