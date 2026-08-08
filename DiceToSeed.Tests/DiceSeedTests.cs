using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The dice convention end to end: rolls, preimage, SHA-256, truncation, checksum, words.
///
/// Every SHA-256 value asserted here was recomputed with sha256sum and with openssl before
/// being written down, against the exact preimage strings below.
/// </summary>
public class DiceSeedTests
{
    // The 50 and 99 roll logs of the plan: "123456" repeated and cut to length.
    const string Rolls50 = "12345612345612345612345612345612345612345612345612";
    const string Rolls99 = "123456123456123456123456123456123456123456123456123456123456123456123456123456123456123456123456123";

    /// <summary>
    /// Vector 4: Coldcard's published example, 24 words from the rolls 1,2,3,4,5,6. Agreement
    /// here is agreement with the vendor.
    /// </summary>
    [Fact]
    public void Vector_4_coldcard_published_example_at_24_words()
    {
        var derivation = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.TwentyFour, RollSeparator.None).Value;

        Assert.Equal("123456", derivation.Preimage);
        Assert.Equal("8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", derivation.Sha256Hex);
        Assert.Equal("8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", derivation.EntropyHex);
        Assert.Equal(
            "mirror reject rookie talk pudding throw happy era myth already payment own " +
            "sentence push head sting video explain letter bomb casual hotel rather garment",
            string.Join(' ', derivation.Words));
    }

    /// <summary>
    /// Vector 5: the truncation test, and the most valuable test in the suite.
    ///
    /// The entropy for 12 words is the FIRST 16 BYTES of the same hash. It is not the hash of
    /// the hash, and the checksum comes from the truncation rather than from the full hash.
    /// Both of those mistakes still produce twelve valid words.
    /// </summary>
    [Fact]
    public void Vector_5_the_same_rolls_at_12_words_truncate_rather_than_rehash()
    {
        var derivation = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.Twelve, RollSeparator.None).Value;

        Assert.Equal("8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", derivation.Sha256Hex);
        Assert.Equal("8d969eef6ecad3c29a3a629280e686cf", derivation.EntropyHex);
        Assert.Equal(
            "mirror reject rookie talk pudding throw happy era myth already payment owner",
            string.Join(' ', derivation.Words));
    }

    /// <summary>
    /// The structural claim behind vector 5: 12 and 24 words from the same rolls share their
    /// first eleven words and differ at the twelfth. Eleven words is 121 bits, which sits
    /// inside the 128 bits both derivations share; the twelfth word spans the boundary where
    /// the truncated entropy's checksum takes over. Re-hashing at the truncation step breaks
    /// this, and it breaks it visibly.
    /// </summary>
    [Fact]
    public void Twelve_and_twentyfour_words_share_eleven_words_and_differ_at_the_twelfth()
    {
        var twelve = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.Twelve, RollSeparator.None).Value.Words;
        var twentyFour = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.TwentyFour, RollSeparator.None).Value.Words;

        Assert.Equal(twentyFour.Take(11), twelve.Take(11));
        Assert.NotEqual(twentyFour[11], twelve[11]);
    }

    /// <summary>
    /// Vector 6: the Krux dialect. Proves the separator reaches the preimage and changes
    /// everything downstream.
    ///
    /// This vector stops at the hash on purpose. The dash convention has not yet been
    /// confirmed against Krux's own published output, so no word-level expectation is
    /// asserted and the UI does not offer this separator under a vendor's name. See the plan,
    /// section 8, second open item.
    /// </summary>
    [Fact]
    public void Vector_6_the_dash_separator_changes_the_preimage_and_the_hash()
    {
        var dashed = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.Twelve, RollSeparator.Dash).Value;
        var joined = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.Twelve, RollSeparator.None).Value;

        Assert.Equal("1-2-3-4-5-6", dashed.Preimage);
        Assert.Equal("b76c3b0194c3c3b0e31e358d76ea00414bdacb2024c976c8d7963d896017f851", dashed.Sha256Hex);
        Assert.NotEqual(joined.Sha256Hex, dashed.Sha256Hex);
        Assert.Empty(dashed.Words.Intersect(joined.Words.Take(1)));
    }

    /// <summary>Vector 7: the primary case. 50 rolls, 12 words, minimum enforced.</summary>
    [Fact]
    public void Vector_7_fifty_rolls_at_12_words()
    {
        var derivation = DiceSeed.Derive(Rolls50, WordCount.Twelve, RollSeparator.None).Value;

        Assert.Equal(Rolls50, derivation.Preimage);
        Assert.Equal("ee72ae915a4e6ea7ccbeb8e5e5eecef29a1d0d90f053183726a424b6d3b07325", derivation.Sha256Hex);
        Assert.Equal("ee72ae915a4e6ea7ccbeb8e5e5eecef2", derivation.EntropyHex);
        Assert.Equal(
            "unveil nice picture region tragic fault cream strike tourist control recipe tourist",
            string.Join(' ', derivation.Words));
    }

    /// <summary>Vector 8: 99 rolls, 24 words, minimum enforced.</summary>
    [Fact]
    public void Vector_8_ninetynine_rolls_at_24_words()
    {
        var derivation = DiceSeed.Derive(Rolls99, WordCount.TwentyFour, RollSeparator.None).Value;

        Assert.Equal("5588d3630bd19f6375b7bd922457af34ea9c74f00807566a1cf808e445dc8c20", derivation.Sha256Hex);
        Assert.Equal("5588d3630bd19f6375b7bd922457af34ea9c74f00807566a1cf808e445dc8c20", derivation.EntropyHex);
        Assert.Equal(
            "few educate sugar bless boring random strategy waste mutual cargo type hawk " +
            "prefer denial scan abstract filter extend dignity balcony dust unusual correct bubble",
            string.Join(' ', derivation.Words));
    }

    /// <summary>
    /// The hash is of the digits alone. A trailing newline is the classic way to derive a
    /// different wallet from the same rolls, which is why the runbook says printf and not echo.
    /// </summary>
    [Fact]
    public void Nothing_is_appended_to_the_preimage()
    {
        var derivation = DiceSeed.Derive(Rolls50 + "\n", WordCount.Twelve, RollSeparator.None).Value;

        Assert.Equal(Rolls50, derivation.Preimage);
        Assert.Equal("ee72ae915a4e6ea7ccbeb8e5e5eecef29a1d0d90f053183726a424b6d3b07325", derivation.Sha256Hex);
    }

    [Fact]
    public void The_public_entry_point_enforces_the_minimum()
    {
        var result = DiceSeed.Derive("123456", WordCount.Twelve, RollSeparator.None);

        Assert.True(result.IsFailure);
        Assert.Contains("50", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bad_character_fails_before_anything_is_hashed()
    {
        var result = DiceSeed.Derive(Rolls50.Remove(10, 1).Insert(10, "7"), WordCount.Twelve, RollSeparator.None);

        Assert.True(result.IsFailure);
        Assert.Contains("'7'", result.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(WordCount.Twelve, 12, 32)]
    [InlineData(WordCount.TwentyFour, 24, 64)]
    public void Word_count_selects_the_entropy_length(WordCount words, int expectedWords, int expectedEntropyHexLength)
    {
        var derivation = DiceSeed.Derive(Rolls99, words, RollSeparator.None).Value;

        Assert.Equal(expectedWords, derivation.Words.Count);
        Assert.Equal(expectedEntropyHexLength, derivation.EntropyHex.Length);
    }

    /// <summary>
    /// The entropy must be a prefix of the displayed hash. If it ever is not, the app has
    /// re-hashed somewhere, and the user comparing the two hex strings on screen would be the
    /// only one who could notice.
    /// </summary>
    [Theory]
    [InlineData(WordCount.Twelve)]
    [InlineData(WordCount.TwentyFour)]
    public void The_entropy_is_a_prefix_of_the_displayed_hash(WordCount words)
    {
        var derivation = DiceSeed.Derive(Rolls99, words, RollSeparator.None).Value;

        Assert.StartsWith(derivation.EntropyHex, derivation.Sha256Hex, StringComparison.Ordinal);
    }
}
