using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The entropy arithmetic the page quotes. Pinned here because these are the numbers that make
/// the vendor minimums honest or dishonest, and a figure typed into a UI string rots silently.
/// </summary>
public class RollEntropyTests
{
    /// <summary>
    /// The finding that prompted all of this: the 24-word minimum does not reach its target even
    /// with perfect dice. If this test ever passes at 99 rolls, the arithmetic has been changed.
    /// </summary>
    [Fact]
    public void Ninety_nine_fair_rolls_do_not_reach_256_bits()
    {
        Assert.True(RollEntropy.FairBits(99) < 256);
        Assert.Equal(255.9, RollEntropy.FairBits(99), 1);

        // And one more roll clears it, which is how close the vendor number is to sufficient.
        Assert.True(RollEntropy.FairBits(100) > 256);
    }

    [Fact]
    public void Fifty_fair_rolls_do_reach_128_bits()
    {
        Assert.True(RollEntropy.FairBits(50) > 128);
        Assert.Equal(129.2, RollEntropy.FairBits(50), 1);
    }

    /// <summary>
    /// Only the 24-word minimum falls short on fair dice. The page must say so there and must not
    /// cry wolf at 12 words.
    /// </summary>
    [Theory]
    [InlineData(WordCount.Twelve, false)]
    [InlineData(WordCount.TwentyFour, true)]
    public void The_minimum_falls_short_only_at_twenty_four_words(WordCount words, bool fallsShort) =>
        Assert.Equal(fallsShort, RollEntropy.MinimumFallsShortFor(words));

    /// <summary>
    /// Under the conservative measure, the 12-word minimum is under target. This is the number
    /// behind recommending more than 50, and it is why "50 is enough" needs a qualifier rather
    /// than a flat yes.
    /// </summary>
    [Fact]
    public void Fifty_rolls_of_a_biased_die_fall_below_128_bits_of_min_entropy()
    {
        Assert.True(RollEntropy.PessimisticBits(50) < 128);
        Assert.Equal(116.1, RollEntropy.PessimisticBits(50), 1);
    }

    /// <summary>
    /// The recommendations have to clear the target on the conservative measure, or they are not
    /// worth making. This is the whole justification for 60 and 111.
    /// </summary>
    [Theory]
    [InlineData(WordCount.Twelve)]
    [InlineData(WordCount.TwentyFour)]
    public void The_recommended_count_clears_the_target_even_under_the_bias_model(WordCount words)
    {
        var recommended = RollEntropy.RecommendedRollsFor(words);

        Assert.True(RollEntropy.PessimisticBits(recommended) > RollEntropy.TargetBitsFor(words),
            $"{recommended} rolls give {RollEntropy.PessimisticBits(recommended):0.0} bits against a target of {RollEntropy.TargetBitsFor(words)}.");
    }

    /// <summary>
    /// One below each recommendation must fail, otherwise the numbers are padded rather than
    /// derived and a reader cannot check them.
    /// </summary>
    [Theory]
    [InlineData(110, 256)]
    [InlineData(55, 128)]
    public void The_recommendations_are_tight_rather_than_padded(int rollCount, int target) =>
        Assert.True(RollEntropy.PessimisticBits(rollCount) < target,
            $"{rollCount} rolls give {RollEntropy.PessimisticBits(rollCount):0.0} bits, which already clears {target}.");

    /// <summary>
    /// The recommendation never becomes a gate: the minimum stays at the vendor numbers so a
    /// 50-roll device seed can still be reproduced here.
    /// </summary>
    [Theory]
    [InlineData(WordCount.Twelve, 50)]
    [InlineData(WordCount.TwentyFour, 99)]
    public void The_minimum_stays_at_the_vendor_number(WordCount words, int expected)
    {
        Assert.Equal(expected, DiceRolls.MinimumRollsFor(words));
        Assert.True(RollEntropy.RecommendedRollsFor(words) > DiceRolls.MinimumRollsFor(words));
    }

    /// <summary>The bias model's constant is log2(5), the min-entropy of a face at p = 0.20.</summary>
    [Fact]
    public void The_bias_model_is_a_face_at_one_in_five() =>
        Assert.Equal(RollEntropy.MinBitsPerBiasedRoll, Math.Log2(5), 12);
}
