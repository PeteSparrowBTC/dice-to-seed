using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// Reading the roll log. Every failure here is an expected input rather than an exceptional
/// condition (a typo, a short log), so every one of them is a Result and none is an exception.
/// </summary>
public class DiceRollsTests
{
    [Theory]
    [InlineData("123456", "123456")]
    [InlineData("12345 61234 5", "12345612345")] // written in groups, as people record a log
    [InlineData("123\n456", "123456")]
    [InlineData("123\r\n456", "123456")]
    [InlineData("  123456  ", "123456")]
    [InlineData("123\t456", "123456")]
    public void Whitespace_and_line_breaks_are_stripped(string raw, string expected) =>
        Assert.Equal(expected, DiceRolls.Read(raw).Value.Digits);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    public void An_empty_log_fails(string raw)
    {
        var result = DiceRolls.Read(raw);

        Assert.True(result.IsFailure);
        Assert.Contains("no rolls", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // A d6 has no 0, 7, 8 or 9. Rejecting them rather than ignoring them matters: a log typed
    // from a keypad with a stray 0 is a log the user miscounted, and silently dropping the
    // character would hash a string they never rolled.
    [Theory]
    [InlineData("12340", '0')]
    [InlineData("1237", '7')]
    [InlineData("1238", '8')]
    [InlineData("1239", '9')]
    [InlineData("123a", 'a')]
    [InlineData("123,456", ',')]
    [InlineData("1-2-3", '-')]
    public void A_character_outside_one_to_six_fails_and_is_named(string raw, char offender)
    {
        var result = DiceRolls.Read(raw);

        Assert.True(result.IsFailure);
        Assert.Contains(offender.ToString(), result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void The_reported_position_is_the_one_the_user_can_count_to()
    {
        // Whitespace is stripped before positions are counted, so the position is the one the
        // user reaches by counting rolls rather than characters typed.
        var result = DiceRolls.Read("12 34 56 12 9");

        Assert.True(result.IsFailure);
        Assert.Contains("'9'", result.Error, StringComparison.Ordinal);
        Assert.Contains("position 9", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_first_offender_is_the_one_reported()
    {
        var result = DiceRolls.Read("123456780");

        Assert.True(result.IsFailure);
        Assert.Contains("'7'", result.Error, StringComparison.Ordinal);
        Assert.Contains("position 7", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_count_is_the_number_of_rolls() =>
        Assert.Equal(50, DiceRolls.Read(new string('1', 50)).Value.Count);

    [Theory]
    [InlineData(RollSeparator.None, "123456")]
    [InlineData(RollSeparator.Dash, "1-2-3-4-5-6")]
    public void The_preimage_is_assembled_from_the_separator(RollSeparator separator, string expected) =>
        Assert.Equal(expected, DiceRolls.Read("123456").Value.Preimage(separator));

    [Theory]
    [InlineData(WordCount.Twelve, 50)]
    [InlineData(WordCount.TwentyFour, 99)]
    public void The_vendor_minimums_are_50_and_99(WordCount words, int expected) =>
        Assert.Equal(expected, DiceRolls.MinimumRollsFor(words));

    [Theory]
    [InlineData(50, WordCount.Twelve)]
    [InlineData(51, WordCount.Twelve)]
    [InlineData(99, WordCount.TwentyFour)]
    [InlineData(120, WordCount.TwentyFour)]
    public void A_log_at_or_above_the_minimum_is_accepted(int rolls, WordCount words) =>
        Assert.True(DiceRolls.Read(new string('3', rolls)).Value.MeetsMinimumFor(words).IsSuccess);

    [Theory]
    [InlineData(49, WordCount.Twelve, 50)]
    [InlineData(38, WordCount.Twelve, 50)]
    [InlineData(98, WordCount.TwentyFour, 99)]
    [InlineData(50, WordCount.TwentyFour, 99)]
    public void A_short_log_fails_and_names_the_real_problem(int rolls, WordCount words, int minimum)
    {
        var result = DiceRolls.Read(new string('3', rolls)).Value.MeetsMinimumFor(words);

        Assert.True(result.IsFailure);
        Assert.Contains($"{rolls} roll", result.Error, StringComparison.Ordinal);
        Assert.Contains($"{minimum}", result.Error, StringComparison.Ordinal);
        // The message must say why more words do not rescue a short log, because the obvious
        // reaction to "50 needed, 38 given" is to ask for 24 words instead.
        Assert.Contains("not the entropy", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A log longer than the minimum is fine and is not truncated: SHA-256 takes the lot.
    /// This is worth asserting because a "take the first 50" bug would silently discard the
    /// user's extra rolls while still producing words.
    /// </summary>
    [Fact]
    public void A_longer_log_keeps_every_roll() =>
        Assert.Equal(64, DiceRolls.Read(new string('6', 64)).Value.Count);
}
