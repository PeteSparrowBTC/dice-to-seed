using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;

namespace DiceToSeed.Core;

/// <summary>
/// Every value the app puts on screen. All four are shown because all four are what another
/// implementation gets compared against; a tool that displays only the words cannot tell a
/// user which step disagreed.
/// </summary>
public sealed record SeedDerivation(
    string Preimage,
    string Sha256Hex,
    string EntropyHex,
    IReadOnlyList<string> Words);

/// <summary>
/// The dice convention, exactly as Coldcard and SeedSigner use it.
///
///   1. entropy bits = 32 * words / 3, so 128 for 12 words and 256 for 24
///   2. H = SHA-256(the roll digits joined by the separator), with NOTHING appended: no
///      trailing newline, no length prefix, no salt
///   3. entropy = the FIRST (entropy bits / 8) bytes of H. The hash is truncated. It is not
///      hashed again
///   4. checksum = the top (entropy bits / 32) bits of SHA-256(entropy), computed from the
///      truncation and not from H
///   5. entropy || checksum, split into 11 bit groups, indexes the wordlist
///
/// Steps 4 and 5 are ordinary BIP-39 and live in <see cref="Bip39"/>. Steps 1 to 3 are the
/// dice convention, and matching them exactly is the whole point of this app.
///
/// The two ways to get this wrong both produce twelve plausible words: hashing H again at
/// step 3, or taking the checksum from H rather than from the truncated entropy. Vector 5 in
/// the test suite exists to catch precisely that.
/// </summary>
public static class DiceSeed
{
    /// <summary>
    /// Derives from a raw roll log, enforcing the vendor minimum of 50 rolls for 12 words and
    /// 99 for 24. This is the entry point the UI uses.
    /// </summary>
    public static Result<SeedDerivation> Derive(string rolls, WordCount words, RollSeparator separator) =>
        DiceRolls.Read(rolls)
            .Bind(log => log.MeetsMinimumFor(words).Map(() => log))
            .Bind(log => Derive(log, words, separator));

    /// <summary>
    /// The same derivation with the length rule skipped, for the published vectors, which use
    /// roll strings far below any minimum. Deliberately internal: the check the UI uses is not
    /// weakened, and no caller outside this assembly can reach past it.
    /// </summary>
    internal static Result<SeedDerivation> DeriveIgnoringMinimum(string rolls, WordCount words, RollSeparator separator) =>
        DiceRolls.Read(rolls).Bind(log => Derive(log, words, separator));

    static Result<SeedDerivation> Derive(DiceRollLog log, WordCount words, RollSeparator separator) =>
        Bip39WordList.Load()
            .Bind(wordList =>
            {
                var preimage = log.Preimage(separator);

                // ASCII by construction: the log is digits 1 to 6 and the separator is "-".
                // UTF-8 is stated rather than assumed so the encoding is visible at the point
                // where the bytes that become a key are produced.
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(preimage));

                // Truncate. Not re-hash. See the class comment.
                var entropy = hash.Take(EntropyByteCountFor(words)).ToArray();

                return Bip39.ToMnemonic(entropy, wordList)
                    .Map(mnemonic => new SeedDerivation(
                        preimage,
                        Convert.ToHexStringLower(hash),
                        Convert.ToHexStringLower(entropy),
                        mnemonic));
            });

    /// <summary>128 bits for 12 words, 256 for 24: 32 * words / 3, in bytes.</summary>
    static int EntropyByteCountFor(WordCount words) => 32 * (int)words / 3 / 8;
}
