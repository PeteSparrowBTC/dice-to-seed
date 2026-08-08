using System.Security.Cryptography;
using CSharpFunctionalExtensions;

namespace DiceToSeed.Core;

/// <summary>
/// Entropy bytes to BIP-39 words. This is ordinary BIP-39 and nothing else: no seed, no
/// PBKDF2, no BIP-32, no passphrase. The app compares words, so words are where it stops.
///
/// The algorithm, in full:
///   checksum   = the top (entropy bits / 32) bits of SHA-256(entropy)
///   bit string = entropy || checksum
///   each 11 bits, most significant first, is a zero-based index into the 2048 word list
///
/// 128 bits of entropy gives a 4 bit checksum and 132 bits, which is 12 words. 256 bits gives
/// an 8 bit checksum and 264 bits, which is 24 words.
/// </summary>
public static class Bip39
{
    // BIP-39 admits 128 to 256 bits in 32 bit steps. Anything else is rejected rather than
    // handled: a mnemonic no other implementation reproduces is worse than an error message.
    static readonly IReadOnlyList<int> SupportedEntropyByteLengths = [16, 20, 24, 28, 32];

    const int BitsPerWord = 11;

    public static Result<IReadOnlyList<string>> ToMnemonic(IReadOnlyList<byte> entropy, Bip39WordList wordList)
    {
        if (!SupportedEntropyByteLengths.Contains(entropy.Count))
            return Result.Failure<IReadOnlyList<string>>(
                $"BIP-39 entropy must be 16, 20, 24, 28 or 32 bytes; {entropy.Count} given.");

        var bytes = entropy.ToArray();
        var checksumBits = bytes.Length * 8 / 32;

        // The checksum is at most 8 bits for the lengths above, so a single hash byte appended
        // to the entropy holds it, and the whole bit string can then be read from one array.
        // The bits beyond checksumBits are never read: the word count stops exactly at
        // (entropy bits + checksumBits) / 11.
        var withChecksum = bytes.Append(SHA256.HashData(bytes)[0]).ToArray();

        var wordCount = (bytes.Length * 8 + checksumBits) / BitsPerWord;

        var words = Enumerable.Range(0, wordCount)
            .Select(word => Enumerable.Range(0, BitsPerWord)
                .Aggregate(0, (index, offset) => (index << 1) | BitAt(withChecksum, word * BitsPerWord + offset)))
            .Select(index => wordList.Words[index])
            .ToList();

        return words;
    }

    /// <summary>Bit <paramref name="position"/> counted from the most significant bit of byte 0.</summary>
    static int BitAt(IReadOnlyList<byte> bytes, int position) =>
        (bytes[position / 8] >> (7 - position % 8)) & 1;
}
