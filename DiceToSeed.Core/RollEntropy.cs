namespace DiceToSeed.Core;

/// <summary>
/// How much entropy a roll log actually carries, so the page can state it rather than imply it.
///
/// This exists because the vendor minimums are a rounding decision and not a sufficiency proof,
/// and the app was quietly presenting them as though they were. The uncomfortable number:
/// ninety-nine fair rolls carry 255.9 bits, which does not reach the 256 a 24-word seed holds.
/// One roll short, with perfect dice. Nothing is broken by that, but a tool whose job is to be
/// checkable should not round it away.
///
/// Two measures are given because they answer different questions and disagree by a lot.
///
///   Shannon      the average information per roll. The right measure for "how much entropy did
///                I collect", and the one under which bias is almost free
///   min-entropy  the probability of an attacker's single most likely guess. The conservative
///                measure, and the one under which bias bites
///
/// Real guessing cost sits between them, nearer Shannon for a mildly biased source. Min-entropy
/// is quoted so the page can say what the floor is rather than only the average.
/// </summary>
public static class RollEntropy
{
    /// <summary>log2(6). A fair six-sided die.</summary>
    public const double BitsPerFairRoll = 2.584962500721156;

    /// <summary>
    /// The min-entropy of a deliberately pessimistic die: the most likely face at p = 0.20
    /// instead of 0.1667, which is far worse than mass-produced dice are. -log2(0.20) is log2(5).
    ///
    /// A die like that loses only about half a bit of Shannon entropy across a fifty-roll run,
    /// which is the "fraction of a bit" figure the README quotes. Its min-entropy loss is much
    /// larger, and that is the gap the recommended roll counts exist to cover.
    /// </summary>
    public const double MinBitsPerBiasedRoll = 2.321928094887362;

    /// <summary>Entropy from a fair die, in bits.</summary>
    public static double FairBits(int rollCount) => rollCount * BitsPerFairRoll;

    /// <summary>The conservative floor: min-entropy under the pessimistic bias model, in bits.</summary>
    public static double PessimisticBits(int rollCount) => rollCount * MinBitsPerBiasedRoll;

    /// <summary>128 bits behind twelve words, 256 behind twenty-four.</summary>
    public static int TargetBitsFor(WordCount words) => (int)DiceRolls.StrengthFor(words);

    /// <summary>
    /// What to roll when you are creating a new seed rather than reproducing one, chosen so the
    /// conservative floor clears the target rather than the average just about reaching it.
    ///
    ///   12 words  56 rolls clear 128 bits of min-entropy; 60 is that with room and easy to count
    ///   24 words  111 rolls clear 256. The vendor minimum of 99 does not, at any bias
    ///
    /// This is a recommendation and never a gate. <see cref="DiceRolls.MinimumRollsFor(WordCount)"/>
    /// stays at the vendor numbers, because someone reproducing a 50-roll device seed must be able
    /// to, and Coinkite's own advice after the 2026 firmware defect was that seeds of at least 50
    /// rolls were unaffected. Refusing those would turn away the people the app exists for.
    /// </summary>
    public static int RecommendedRollsFor(WordCount words) => words == WordCount.Twelve ? 60 : 111;

    /// <summary>
    /// True when the vendor minimum does not reach the target even with perfect dice, which is
    /// the case at 24 words and not at 12. The page needs to say so at that word count.
    /// </summary>
    public static bool MinimumFallsShortFor(WordCount words) =>
        FairBits(DiceRolls.MinimumRollsFor(words)) < TargetBitsFor(words);
}
