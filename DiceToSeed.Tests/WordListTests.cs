using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The wordlist is the one input to this app that was not written here, so it is the one that
/// gets checked at runtime. A single altered word silently produces a different seed for the
/// same rolls, and the user has no way to notice: the words would still look like words.
///
/// The expected hash is the SHA-256 of the canonical BIP-39 English list as published in the
/// bitcoin/bips repository, LF line endings, trailing newline included. .gitattributes pins
/// *.txt to eol=lf so a Windows checkout cannot change it.
/// </summary>
public class WordListTests
{
    [Fact]
    public void The_embedded_list_matches_the_published_sha256() =>
        Assert.Equal(
            "2f5eed53a4727b4bf8880d8f3f199efc90e58503646d9ff8eff3a2ed3b24dbda",
            Bip39WordList.Load().Value.Sha256Hex);

    [Fact]
    public void The_embedded_list_holds_exactly_2048_words() =>
        Assert.Equal(2048, Bip39WordList.Load().Value.Words.Count);

    [Fact]
    public void Loading_succeeds() =>
        Assert.True(Bip39WordList.Load().IsSuccess, Bip39WordList.Load().IsFailure ? Bip39WordList.Load().Error : string.Empty);

    // The BIP-39 list is sorted, and every word is unique. Both properties are what make the
    // 11-bit index unambiguous, so they are worth asserting rather than assuming.
    [Fact]
    public void The_list_is_sorted_and_free_of_duplicates()
    {
        var words = Bip39WordList.Load().Value.Words;

        Assert.Equal(words.OrderBy(w => w, StringComparer.Ordinal), words);
        Assert.Equal(words.Count, words.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(0, "abandon")]
    [InlineData(1, "ability")]
    [InlineData(2047, "zoo")]
    public void Known_indexes_hold_known_words(int index, string expected) =>
        Assert.Equal(expected, Bip39WordList.Load().Value.Words[index]);

    /// <summary>
    /// The published constant and the value the code compares against must be the same string.
    /// If someone updates one and not the other the check still passes and means nothing.
    /// </summary>
    [Fact]
    public void The_expected_hash_constant_is_the_value_that_is_checked() =>
        Assert.Equal(Bip39WordList.ExpectedSha256Hex, Bip39WordList.Load().Value.Sha256Hex);
}
