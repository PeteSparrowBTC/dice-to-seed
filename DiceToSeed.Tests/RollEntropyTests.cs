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

    /// <summary>
    /// Weldon's measurement has to be a probability distribution before anything derived from it
    /// means what the page says it means.
    /// </summary>
    [Fact]
    public void The_measured_die_is_a_distribution_over_six_faces()
    {
        Assert.Equal(6, RollEntropy.MeasuredFaceProbabilities.Count);
        Assert.Equal(1.0, RollEntropy.MeasuredFaceProbabilities.Sum(), 12);
        Assert.All(RollEntropy.MeasuredFaceProbabilities, p => Assert.InRange(p, 0.0, 1.0));

        // Two high faces above fair, four below, which is the direction the pip cavities predict.
        Assert.Equal(2, RollEntropy.MeasuredFaceProbabilities.Count(p => p > RollEntropy.FairFaceProbability));
        Assert.Equal(4, RollEntropy.MeasuredFaceProbabilities.Count(p => p < RollEntropy.FairFaceProbability));
    }

    /// <summary>
    /// The claim the page makes about ordinary dice, as a number: the largest bias ever measured
    /// on pipped dice costs about one bit of min-entropy across a sixty-roll log, and a few
    /// thousandths of a bit on the average measure. This is the assertion that replaced the
    /// unbacked words "worse than real dice are".
    /// </summary>
    [Fact]
    public void The_measured_bias_costs_about_one_bit_across_a_sixty_roll_log()
    {
        Assert.Equal(0.16885, RollEntropy.MeasuredTopFaceProbability, 8);

        var minEntropyCost = RollEntropy.FairBits(60) - RollEntropy.MeasuredBits(60);
        Assert.Equal(1.1, minEntropyCost, 1);

        // On the average measure the same die costs three orders of magnitude less, because the
        // Shannon penalty is quadratic in the deviation while the min-entropy penalty is linear.
        Assert.Equal(0.004, RollEntropy.MeasuredShannonShortfall(60), 3);
        Assert.True(minEntropyCost / RollEntropy.MeasuredShannonShortfall(60) > 100);
    }

    /// <summary>
    /// A real die still clears both targets at the recommended count, and clears 128 bits even at
    /// the 12-word minimum. If this ever fails, the recommendation is no longer a margin over
    /// reality and the page must stop saying ordinary dice are sufficient.
    /// </summary>
    [Theory]
    [InlineData(50, 128)]
    [InlineData(60, 128)]
    [InlineData(111, 256)]
    public void A_measured_real_die_clears_the_target(int rollCount, int target) =>
        Assert.True(RollEntropy.MeasuredBits(rollCount) > target,
            $"{rollCount} rolls of a die with the bias Weldon measured give {RollEntropy.MeasuredBits(rollCount):0.0} bits against {target}.");

    /// <summary>
    /// The 99-roll minimum falls short of 256 on a real die too, for the same reason it does on a
    /// perfect one: the roll count, not the dice.
    /// </summary>
    [Fact]
    public void Ninety_nine_rolls_fall_short_of_256_on_a_real_die_as_well()
    {
        Assert.True(RollEntropy.MeasuredBits(99) < 256);
        Assert.True(RollEntropy.FairBits(99) < 256);
    }

    /// <summary>
    /// The pessimistic column is fifteen times more lopsided than the measurement, and the page
    /// says "fifteen" in words. Pinned so the prose and the arithmetic cannot part company.
    /// </summary>
    [Fact]
    public void The_pessimistic_die_is_fifteen_times_more_lopsided_than_a_measured_one()
    {
        Assert.InRange(RollEntropy.PessimismFactor, 15.0, 16.0);
        Assert.Equal(15.3, RollEntropy.PessimismFactor, 1);
    }

    /// <summary>
    /// The three models have to be ordered, at every roll count the page shows, or the table is
    /// telling a story the numbers do not support.
    /// </summary>
    [Theory]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(99)]
    [InlineData(111)]
    public void Fair_beats_measured_beats_pessimistic(int rollCount)
    {
        Assert.True(RollEntropy.FairBits(rollCount) > RollEntropy.MeasuredBits(rollCount));
        Assert.True(RollEntropy.MeasuredBits(rollCount) > RollEntropy.PessimisticBits(rollCount));
    }
}
