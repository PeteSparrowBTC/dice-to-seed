using CSharpFunctionalExtensions;

namespace DiceToSeed.Core;

/// <summary>How many words to derive. The value is the word count itself.</summary>
public enum WordCount
{
    Twelve = 12,
    TwentyFour = 24,
}

/// <summary>
/// Reading a roll log. Character validation and length validation are deliberately separate:
/// the UI shows a live roll counter and a live preimage while the user is still typing, so it
/// needs a log that is well formed but not yet long enough.
/// </summary>
public static class DiceRolls
{
    /// <summary>
    /// The vendor minimums, not this app's opinion. Coldcard asks for 50 rolls for 12 words
    /// and 99 for 24, and the job here is to agree with the vendors rather than to editorialise.
    /// </summary>
    public static int MinimumRollsFor(WordCount words) =>
        words == WordCount.Twelve ? 50 : 99;

    /// <summary>
    /// Strips whitespace, then rejects anything that is not a d6 face. No length rule is
    /// applied here; see <see cref="DiceRollLog.MeetsMinimumFor"/>.
    /// </summary>
    public static Result<DiceRollLog> Read(string raw)
    {
        var digits = new string((raw ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());

        if (digits.Length == 0)
            return Result.Failure<DiceRollLog>("There are no rolls to convert. Enter the log of six-sided dice rolls.");

        // Report the first offender with its position, counting from 1 over the stripped
        // digits, which is the number the user gets if they count along their own log. A
        // character outside 1-6 is never dropped silently: a stray key means the log on paper
        // and the log on screen have diverged, and that is exactly what must not be hashed.
        var offender = digits
            .Select((character, index) => (character, position: index + 1))
            .Where(entry => entry.character is < '1' or > '6')
            .Select(entry => (int?)entry.position)
            .FirstOrDefault();

        return offender is { } position
            ? Result.Failure<DiceRollLog>(
                $"'{digits[position - 1]}' at position {position} is not a six-sided die face. Only the digits 1 to 6 are rolls; a d6 has no 0 and no 7, 8 or 9.")
            : new DiceRollLog(digits);
    }
}

/// <summary>A validated log of d6 rolls: digits 1 to 6, whitespace already removed.</summary>
public sealed record DiceRollLog
{
    internal DiceRollLog(string digits) => Digits = digits;

    public string Digits { get; }

    public int Count => Digits.Length;

    /// <summary>
    /// The exact string that goes into SHA-256: the roll digits, joined by nothing at all. The
    /// UI renders this character for character, because it is what another tool gets compared
    /// against.
    ///
    /// There is no separator option, and that is a finding rather than an omission. For d6,
    /// Coldcard, SeedSigner and Krux all hash the bare digit string:
    ///
    ///   Coldcard  docs/rolls12.py:            sha256(r.encode()).digest()[:16]
    ///   SeedSigner mnemonic_generation.py:    hashlib.sha256(roll_data.encode()).digest()
    ///   Krux      new_mnemonic/dice_rolls.py: "".join(self.rolls) if self.num_sides &lt; 10
    ///                                          else "-".join(self.rolls)
    ///
    /// Krux's dash applies when a face value can exceed one digit, which is d20, where "1"
    /// then "2" and "12" would otherwise be the same string. This app takes d6 only, so the
    /// dash branch is unreachable for every device it verifies against. A separator control
    /// here would have exactly one setting that no vendor reproduces, in an app whose purpose
    /// is agreeing with vendors. If d20 is ever added, the separator returns with it and with
    /// its own published vectors.
    /// </summary>
    public string Preimage => Digits;

    /// <summary>
    /// The length rule. The message says why a longer word count does not rescue a short log,
    /// because that is the first thing someone reaches for when told 50 rolls are needed.
    /// </summary>
    public Result MeetsMinimumFor(WordCount words)
    {
        var minimum = DiceRolls.MinimumRollsFor(words);

        return Count >= minimum
            ? Result.Success()
            : Result.Failure(
                $"{Count} rolls is below the {minimum}-roll minimum for {(int)words} words. A short log does not become stronger by producing more words; the word count is not the entropy. Roll {minimum - Count} more.");
    }
}
