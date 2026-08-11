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
    /// Why a rolling test cannot replace looking at the die, as elementary arithmetic rather than a
    /// power table, so that a reader can check it with a calculator.
    ///
    /// At the recommended sixty rolls, the pessimistic die's most likely face is expected twelve
    /// times against a fair ten. The standard deviation of that count is 2.9, so the excess of two
    /// is under one deviation: the thing being looked for is smaller than the noise it must be seen
    /// against.
    /// </summary>
    [Fact]
    public void At_sixty_rolls_a_twenty_percent_bias_is_under_one_deviation()
    {
        Assert.Equal(10, RollEntropy.ExpectedFaceCount(60), 6);
        Assert.Equal(2.9, RollEntropy.FaceCountDeviation(60), 1);

        var signal = RollEntropy.BiasSignalInDeviations(60, RollEntropy.PessimisticFaceProbability);

        Assert.True(signal < 1, $"The excess stands {signal:0.00} deviations clear, which would make a rolling test worth running.");
        Assert.Equal(0.7, signal, 1);
    }

    /// <summary>
    /// And what it would take instead. The signal grows as the square root of the roll count, so
    /// three deviations needs about eleven hundred rolls, which is nobody's ceremony. This is the
    /// number behind telling people to inspect the die rather than test their log.
    /// </summary>
    [Fact]
    public void Three_deviations_would_need_about_eleven_hundred_rolls()
    {
        var needed = RollEntropy.RollsForSignalInDeviations(3, RollEntropy.PessimisticFaceProbability);

        Assert.InRange(needed, 1000, 1200);

        // Round trip: at that count the signal really does reach three deviations, and one roll
        // fewer does not, so the inversion is not approximately right in a way that hides an error.
        Assert.True(RollEntropy.BiasSignalInDeviations(needed, RollEntropy.PessimisticFaceProbability) >= 3);
        Assert.True(RollEntropy.BiasSignalInDeviations(needed - 1, RollEntropy.PessimisticFaceProbability) < 3);
    }

    /// <summary>
    /// The neatest fact in this file, and it arrived by getting an assertion wrong: detecting the
    /// bias Weldon measured takes about 262,000 rolls, and Weldon's dataset is 315,672 rolls.
    ///
    /// That is not a coincidence, it is the same calculation from the other end. The reason the only
    /// solid measurement of ordinary dice bias comes from a Victorian throwing dice a quarter of a
    /// million times is that a quarter of a million rolls is what the measurement costs. Nobody can
    /// check their own die by rolling it, and this is the number that says so.
    /// </summary>
    [Fact]
    public void Detecting_the_measured_bias_needs_about_as_many_rolls_as_Weldon_threw()
    {
        var needed = RollEntropy.RollsForSignalInDeviations(3, RollEntropy.MeasuredTopFaceProbability);

        Assert.InRange(needed, 250_000, 275_000);

        // Within the same order of magnitude as the dataset the figure came from, and below it, which
        // is why Weldon's count was enough to see the effect at all.
        Assert.True(needed < RollEntropy.WeldonRollCount,
            $"{needed} rolls needed against Weldon's {RollEntropy.WeldonRollCount}, so his data could not have shown it.");
    }

    /// <summary>
    /// The multiset count, which is the whole argument for using one die. Checked against values
    /// small enough to enumerate by hand so the stepwise binomial cannot be quietly wrong.
    /// </summary>
    [Theory]
    [InlineData(1, 6)]     // one die: six outcomes, and nothing to order
    [InlineData(2, 21)]    // {1,1}..{6,6}: 21 unordered pairs against 36 ordered
    [InlineData(4, 126)]
    [InlineData(5, 252)]
    public void Unordered_outcomes_are_the_multiset_count(int dice, long expected) =>
        Assert.Equal(expected, RollEntropy.UnorderedOutcomes(dice));

    /// <summary>
    /// A single die loses nothing to ordering, because the order is the order you threw them in.
    /// This is the sanity check that the advice and the arithmetic agree.
    /// </summary>
    [Fact]
    public void One_die_cannot_lose_anything_to_ordering()
    {
        Assert.Equal(0, RollEntropy.FractionLostWithoutOrder(1));
        Assert.Equal(RollEntropy.BitsPerFairRoll, RollEntropy.UnorderedThrowBits(1), 12);
        Assert.Equal(RollEntropy.FairBits(60), RollEntropy.BitsWithoutOrder(60, 1), 12);
    }

    /// <summary>
    /// The number the copy quotes: throwing five identical dice at once and recording what they show,
    /// without establishing which die is which, costs well over a third of the throw.
    /// </summary>
    [Fact]
    public void Five_dice_thrown_together_lose_over_a_third_of_the_throw()
    {
        Assert.Equal(12.9, RollEntropy.OrderedThrowBits(5), 1);
        Assert.Equal(8.0, RollEntropy.UnorderedThrowBits(5), 1);
        Assert.InRange(RollEntropy.FractionLostWithoutOrder(5), 0.38, 0.39);

        // Four is the count the README quotes as "about a third".
        Assert.InRange(RollEntropy.FractionLostWithoutOrder(4), 0.32, 0.34);
    }

    /// <summary>
    /// And the consequence that matters: a log rolled to the recommended length this way does not
    /// reach the target it was rolled for, while the counter reads the full count and the words look
    /// exactly as plausible. Nothing downstream can catch it, which is why it is a warning rather
    /// than a check.
    /// </summary>
    [Fact]
    public void A_recommended_length_log_thrown_five_at_a_time_falls_below_its_target()
    {
        var recommended = RollEntropy.RecommendedRollsFor(WordCount.Twelve);
        var target = RollEntropy.TargetBitsFor(WordCount.Twelve);

        Assert.True(RollEntropy.FairBits(recommended) > target);
        Assert.True(RollEntropy.BitsWithoutOrder(recommended, 5) < target,
            $"{recommended} rolls thrown five at a time without order give {RollEntropy.BitsWithoutOrder(recommended, 5):0.0} bits against {target}.");
        Assert.Equal(95.7, RollEntropy.BitsWithoutOrder(recommended, 5), 1);
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
