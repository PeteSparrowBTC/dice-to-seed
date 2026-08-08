using System.Security.Cryptography;
using System.Text.Json;
using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// Entropy to words, with the dice step bypassed. These test the BIP-39 half on its own, which
/// is where an off-by-one in the 11-bit split would hide: such a bug still produces twelve
/// plausible English words, and only a published vector catches it.
/// </summary>
public class Bip39Tests
{
    static Bip39WordList WordList => Bip39WordList.Load().Value;

    static string Mnemonic(string entropyHex) =>
        string.Join(' ', Bip39.ToMnemonic(Convert.FromHexString(entropyHex), WordList).Value);

    /// <summary>
    /// The three vectors named in the plan, spelled out here so the file can be read against
    /// the plan without opening the fixture. The whole published set runs below.
    /// </summary>
    [Theory]
    [InlineData("00000000000000000000000000000000",
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about")]
    [InlineData("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f",
        "legal winner thank year wave sausage worth useful legal winner thank yellow")]
    [InlineData("80808080808080808080808080808080",
        "letter advice cage absurd amount doctor acoustic avoid letter advice cage above")]
    public void The_official_english_vectors_named_in_the_plan(string entropyHex, string expected) =>
        Assert.Equal(expected, Mnemonic(entropyHex));

    /// <summary>
    /// The whole published English set, 16, 24 and 32 byte entropy alike. The 32 byte cases are
    /// the ones that matter for 24 word dice seeds.
    /// </summary>
    [Fact]
    public void Every_published_english_vector_round_trips()
    {
        var failures = PublishedEnglishVectors()
            .Where(vector => Mnemonic(vector.EntropyHex) != vector.Mnemonic)
            .Select(vector => $"{vector.EntropyHex}: expected \"{vector.Mnemonic}\", got \"{Mnemonic(vector.EntropyHex)}\"")
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void The_published_vector_set_is_not_empty() =>
        Assert.True(PublishedEnglishVectors().Count >= 24,
            $"Expected the published English vector set, found {PublishedEnglishVectors().Count} entries.");

    /// <summary>
    /// Proves the fixture is the file upstream publishes rather than one edited until the
    /// tests passed. A vector suite that can be quietly adjusted proves nothing at all.
    /// </summary>
    [Fact]
    public void The_vector_fixture_matches_its_published_sha256() =>
        Assert.Equal(
            "fa3b937b7cff9c9b8ecd3aa011faeb8d6dd67993174b72326e83f4de8fdb30f8",
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(VectorFilePath()))));

    [Theory]
    [InlineData(16, 12)]
    [InlineData(20, 15)]
    [InlineData(24, 18)]
    [InlineData(28, 21)]
    [InlineData(32, 24)]
    public void Entropy_length_determines_word_count(int entropyBytes, int expectedWords) =>
        Assert.Equal(expectedWords, Bip39.ToMnemonic(new byte[entropyBytes], WordList).Value.Count);

    // BIP-39 admits 128 to 256 bits in 32 bit steps and nothing else. A length outside that
    // set is a caller mistake, and the caller gets told rather than given a mnemonic that no
    // other implementation will reproduce.
    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(33)]
    public void An_unsupported_entropy_length_fails_rather_than_guessing(int entropyBytes)
    {
        var result = Bip39.ToMnemonic(new byte[entropyBytes], WordList);

        Assert.True(result.IsFailure);
        Assert.Contains("entropy", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The checksum is the top bits of SHA-256 of the entropy, so flipping the last entropy
    /// bit must change the final word. This is the property vector 5 of the plan relies on.
    /// </summary>
    [Fact]
    public void A_one_bit_change_in_entropy_changes_the_last_word()
    {
        var entropy = Convert.FromHexString("8d969eef6ecad3c29a3a629280e686cf");
        var flipped = entropy.ToArray();
        flipped[^1] ^= 0x01;

        var original = Bip39.ToMnemonic(entropy, WordList).Value;
        var changed = Bip39.ToMnemonic(flipped, WordList).Value;

        Assert.NotEqual(original[^1], changed[^1]);
    }

    sealed record PublishedVector(string EntropyHex, string Mnemonic);

    static IReadOnlyList<PublishedVector> PublishedEnglishVectors() =>
        JsonDocument.Parse(File.ReadAllBytes(VectorFilePath()))
            .RootElement.GetProperty("english")
            .EnumerateArray()
            .Select(entry => new PublishedVector(entry[0].GetString()!, entry[1].GetString()!))
            .ToList();

    /// <summary>
    /// The fixture is copied next to the test binary by the project file, so this resolves
    /// against the output directory rather than walking the source tree.
    /// </summary>
    static string VectorFilePath() =>
        Path.Combine(AppContext.BaseDirectory, "Vectors", "bip39-english-vectors.json");
}
