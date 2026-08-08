using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;

namespace DiceToSeed.Core;

/// <summary>
/// The BIP-39 English wordlist, embedded in this assembly and verified against its published
/// SHA-256 every time it is loaded.
///
/// Why verify at all: this is the only input to the app that was not written in this
/// repository. One altered word maps an index to the wrong word, which produces a valid
/// looking mnemonic for a different key, and nothing on screen would look wrong. The check
/// costs a hash of 13 KB and removes that whole class of failure.
///
/// The hash covers the file exactly as published in bitcoin/bips: 2048 lines, LF endings, a
/// trailing newline. .gitattributes pins *.txt to eol=lf so a Windows checkout cannot alter
/// it; without that pin every clone on Windows would fail this check for a reason that has
/// nothing to do with the contents.
/// </summary>
public sealed class Bip39WordList
{
    public const string ExpectedSha256Hex = "2f5eed53a4727b4bf8880d8f3f199efc90e58503646d9ff8eff3a2ed3b24dbda";

    public const int ExpectedWordCount = 2048;

    const string ResourceName = "DiceToSeed.Core.WordList.english.txt";

    Bip39WordList(IReadOnlyList<string> words, string sha256Hex) =>
        (Words, Sha256Hex) = (words, sha256Hex);

    public IReadOnlyList<string> Words { get; }

    /// <summary>The hash of the embedded bytes, shown in the UI so a user can confirm it themselves.</summary>
    public string Sha256Hex { get; }

    /// <summary>
    /// Loads and verifies the list. A failure here means the app must not derive anything, so
    /// it is returned as a <see cref="Result"/> for the UI to render, not thrown: a corrupted
    /// resource is a state the user needs explained, not a stack trace.
    /// </summary>
    public static Result<Bip39WordList> Load()
    {
        var bytes = ReadEmbeddedBytes();

        if (bytes is null)
            return Result.Failure<Bip39WordList>(
                $"The embedded wordlist resource '{ResourceName}' is missing from this build. Nothing can be derived.");

        var sha256Hex = Convert.ToHexStringLower(SHA256.HashData(bytes));

        if (sha256Hex != ExpectedSha256Hex)
            return Result.Failure<Bip39WordList>(
                $"The embedded wordlist does not match the published BIP-39 English list. Expected SHA-256 {ExpectedSha256Hex}, found {sha256Hex}. Nothing can be derived.");

        // Split on both endings so a mangled checkout produces the word-count message rather
        // than 2048 words with a stray carriage return welded onto each one. In practice the
        // hash check above has already rejected that case; this keeps the failure legible if
        // the expected hash is ever updated without the surrounding reasoning.
        var words = Encoding.UTF8.GetString(bytes)
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        return words.Length == ExpectedWordCount
            ? new Bip39WordList(words, sha256Hex)
            : Result.Failure<Bip39WordList>(
                $"The embedded wordlist holds {words.Length} words, not {ExpectedWordCount}. Nothing can be derived.");
    }

    static byte[]? ReadEmbeddedBytes()
    {
        using var stream = typeof(Bip39WordList).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName);

        if (stream is null)
            return null;

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}
