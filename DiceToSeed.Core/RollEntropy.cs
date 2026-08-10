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
///
/// Three dice are modelled, not two, because the page used to say the pessimistic model was
/// "worse than real dice are" without a number, which is exactly the kind of unbacked claim this
/// repository exists not to make. The middle column is a measurement.
/// </summary>
public static class RollEntropy
{
    /// <summary>log2(6). A fair six-sided die.</summary>
    public const double BitsPerFairRoll = 2.584962500721156;

    /// <summary>A fair face.</summary>
    public const double FairFaceProbability = 1.0 / 6;

    /// <summary>
    /// The most likely face of the deliberately pessimistic die, at p = 0.20 instead of 0.1667.
    /// A 20% relative excess on one face, which is fifteen times the largest excess ever measured
    /// on ordinary pipped dice. See <see cref="PessimismFactor"/>.
    /// </summary>
    public const double PessimisticFaceProbability = 0.20;

    /// <summary>
    /// The min-entropy of the pessimistic die. -log2(0.20), which is log2(5).
    ///
    /// A die like that loses only about half a bit of Shannon entropy across a fifty-roll run,
    /// which is the "fraction of a bit" figure the README quotes. Its min-entropy loss is much
    /// larger, and that is the gap the recommended roll counts exist to cover.
    /// </summary>
    public const double MinBitsPerBiasedRoll = 2.321928094887362;

    /// <summary>
    /// Raphael Weldon's dice data, 1894: twelve dice thrown 26,306 times, so 315,672 individual
    /// rolls, recording how many showed a 5 or a 6. The proportion came out at 0.3377 against an
    /// expected 0.3333. That is the largest published count on ordinary pipped dice, and the
    /// dataset Pearson and Fisher both used, which is why it is the number quoted here rather
    /// than a guess about manufacturing tolerance.
    ///
    /// The direction is not an accident and not a defect: a pipped die has its spots drilled out
    /// and filled with lighter paint, so the six face carries the most missing mass and the die
    /// lands on it slightly more often than one time in six. Precision casino dice have flush
    /// pips of matched density for this reason.
    /// </summary>
    public const double WeldonHighPairFrequency = 0.3377;

    /// <summary>How many individual rolls that frequency was measured over.</summary>
    public const int WeldonRollCount = 315_672;

    /// <summary>
    /// Weldon's measurement spread over six faces. He recorded only "5 or 6" against "not 5 or 6",
    /// so the excess is attributed to the two high faces equally and the deficit to the other
    /// four. That is a modelling assumption; it is the one the pip-cavity explanation predicts,
    /// and it is deliberately the pessimistic reading of his data, since concentrating the whole
    /// excess on one face is what makes min-entropy worst.
    /// </summary>
    public static IReadOnlyList<double> MeasuredFaceProbabilities { get; } =
    [
        (1 - WeldonHighPairFrequency) / 4, (1 - WeldonHighPairFrequency) / 4,
        (1 - WeldonHighPairFrequency) / 4, (1 - WeldonHighPairFrequency) / 4,
        WeldonHighPairFrequency / 2, WeldonHighPairFrequency / 2,
    ];

    /// <summary>The most likely face of a measured real die: 0.16885, against a fair 0.16667.</summary>
    public static double MeasuredTopFaceProbability => MeasuredFaceProbabilities.Max();

    /// <summary>
    /// Min-entropy per roll of a measured real die, 2.5662 bits against a fair 2.5850. The whole
    /// answer to "are ordinary dice good enough" is the size of that gap: 0.0188 bits a roll, so
    /// about one bit across a sixty-roll log, on the harshest measure there is.
    /// </summary>
    public static double MinBitsPerMeasuredRoll => -Math.Log2(MeasuredTopFaceProbability);

    /// <summary>
    /// Shannon entropy per roll of a measured real die: 2.58490 bits against a fair 2.58496. The
    /// average measure barely notices a bias this size, because the penalty is quadratic in the
    /// deviation. Across a sixty-roll log it costs four thousandths of a bit.
    /// </summary>
    public static double ShannonBitsPerMeasuredRoll =>
        -MeasuredFaceProbabilities.Sum(p => p * Math.Log2(p));

    /// <summary>
    /// How much more lopsided the pessimistic die is than the measured one, comparing each one's
    /// excess on its most likely face. Comes out just over fifteen. This is the number that turns
    /// "worse than real dice are" from an assurance into a statement someone can check.
    /// </summary>
    public static double PessimismFactor =>
        (PessimisticFaceProbability - FairFaceProbability)
        / (MeasuredTopFaceProbability - FairFaceProbability);

    /// <summary>Entropy from a fair die, in bits.</summary>
    public static double FairBits(int rollCount) => rollCount * BitsPerFairRoll;

    /// <summary>Min-entropy from a die with the bias Weldon measured, in bits.</summary>
    public static double MeasuredBits(int rollCount) => rollCount * MinBitsPerMeasuredRoll;

    /// <summary>
    /// What the measured bias costs on the average measure across a log, in bits. Quoted next to
    /// the min-entropy cost because the two differ by a factor of about three hundred, and citing
    /// only one of them is how this subject gets misrepresented in both directions.
    /// </summary>
    public static double MeasuredShannonShortfall(int rollCount) =>
        rollCount * (BitsPerFairRoll - ShannonBitsPerMeasuredRoll);

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
