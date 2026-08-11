namespace DiceToSeed.Core;

/// <summary>
/// What the app is deriving. Two modes, and they produce results that differ in kind: seed mode
/// renders BIP-39 words, key mode renders hex and never words.
///
/// That difference is what makes a mode selector safe to offer at all. A control whose wrong
/// position still produces plausible output is Ian Coleman's "Dice versus Base 10" trap, which
/// yields a different wallet with no warning. Words and hex cannot be mistaken for one another, so
/// a mis-set mode announces itself. See CLAUDE.md rule 9.
/// </summary>
public enum DerivationMode
{
    SeedPhrase,
    BackupKey,
}

/// <summary>
/// Moving between the two modes, and the rule that one roll log must never produce both.
///
/// This lives in Core rather than in the page, and the reason is worth stating. The rule was
/// implemented correctly in the Razor component and tested nowhere: a review found that the
/// hazard it protects against is pinned by <c>BackupKeyTests</c>, while the protection itself was
/// a few lines of view code that nothing would notice the removal of. For a property this load
/// bearing that is the wrong arrangement, so the decision moved to where it can be asserted.
///
/// The hazard, restated because it is the reason for all of it: <c>k</c> is SHA-256 of the roll
/// log, and a 24-word seed's BIP-39 entropy is that same hash byte for byte (on 12 words, its
/// first half). So one log used for both makes the backup key derivable from the wallet it is
/// meant to protect, and the shares stop protecting anything. <c>BackupKeyTests</c> asserts that
/// equality directly, so if the derivation ever moves, the warnings built on it get revisited.
/// </summary>
public static class ModeSwitch
{
    /// <summary>
    /// Whether the user has to be asked before this switch happens, which is exactly when there is
    /// a log to lose: the mode is really changing and rolls have been recorded.
    ///
    /// The confirmation exists to give the reason rather than to obstruct. Nothing is preserved by
    /// answering yes, and cancelling is a real way out.
    /// </summary>
    public static bool RequiresConfirmation(DerivationMode current, DerivationMode requested, int recordedRolls) =>
        requested != current && recordedRolls > 0;

    /// <summary>
    /// The transition. Returns the mode to be in and the roll log that survives it, which is
    /// nothing at all whenever the mode actually changed.
    ///
    /// A request for the mode already in effect is not a change and keeps the log, so that pressing
    /// the button you are already on cannot destroy fifty rolls.
    /// </summary>
    public static (DerivationMode Mode, string Rolls) Apply(
        DerivationMode current, DerivationMode requested, string rolls) =>
        requested == current
            ? (current, rolls)
            : (requested, string.Empty);
}
