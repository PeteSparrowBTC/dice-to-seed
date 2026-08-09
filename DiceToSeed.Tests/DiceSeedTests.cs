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
        var derivation = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.TwentyFour).Value;

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
        var derivation = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.Twelve).Value;

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
        var twelve = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.Twelve).Value.Words;
        var twentyFour = DiceSeed.DeriveIgnoringMinimum("123456", WordCount.TwentyFour).Value.Words;

        Assert.Equal(twentyFour.Take(11), twelve.Take(11));
        Assert.NotEqual(twentyFour[11], twelve[11]);
    }

    /// <summary>
    /// Vector 9: SeedSigner's own published 50-roll example, from docs/dice_verification.md.
    ///
    /// This is the vector that makes the app's premise checkable. It comes from a different
    /// vendor than vectors 4 and 5, and Coldcard's rolls12.py was run against this same roll
    /// string and printed these same twelve words. Two vendors, one result, and this app
    /// agrees with both.
    /// </summary>
    [Fact]
    public void Vector_9_seedsigner_published_example_at_12_words()
    {
        const string rolls = "65515223131652132161133154444123616466443112153441";

        var derivation = DiceSeed.Derive(rolls, WordCount.Twelve).Value;

        Assert.Equal(rolls, derivation.Preimage);
        Assert.Equal("6cb09af855050dcde6fe2adc3181c250982011e2cf17821cbed56a908ec527c3", derivation.Sha256Hex);
        Assert.Equal(
            "hole luggage safe present express tragic orbit shed switch metal identify path",
            string.Join(' ', derivation.Words));
    }

    /// <summary>Vector 10: SeedSigner's published 99-roll example, same document.</summary>
    [Fact]
    public void Vector_10_seedsigner_published_example_at_24_words()
    {
        const string rolls = "655152231316521321611331544441236164664431121534415633526456254462245546236542364246312613322234612";

        var derivation = DiceSeed.Derive(rolls, WordCount.TwentyFour).Value;

        Assert.Equal("51531761ec7a738946e0b9f46bb11320a695495430e345c14f01ad8b3b898a6d", derivation.Sha256Hex);
        Assert.Equal(
            "eyebrow obvious such suggest poet seven breeze blame virtual frown dynamic donor " +
            "harsh pigeon express broccoli easy apology scatter force recipe shadow claim radio",
            string.Join(' ', derivation.Words));
    }

    /// <summary>
    /// Vector 11: fifty 1s. Not published by any vendor, but pinned here because it is the log
    /// a person reaches for when checking a build by hand: it is the fastest thing to enter,
    /// on a device or in this app, and the easiest to get right.
    ///
    /// The expectation is Coldcard's, not this repository's arithmetic. It was produced by
    /// running Coldcard's own rolls12.py, and the hash was confirmed with both sha256sum and
    /// openssl before being written down.
    ///
    /// It also happens to be the log most likely to tempt someone into re-rolling because it
    /// "looks wrong". It is not wrong. Fifty 1s is exactly as probable as any other fifty
    /// rolls, and a seed derived from it is exactly as strong.
    /// </summary>
    [Fact]
    public void Vector_11_fifty_ones_the_hand_check_log()
    {
        var derivation = DiceSeed.Derive(new string('1', 50), WordCount.Twelve).Value;

        Assert.Equal("3dac51a65ec9fcfc409a1b5f1defe92ba723843118ea511971ab46b36859495f", derivation.Sha256Hex);
        Assert.Equal("3dac51a65ec9fcfc409a1b5f1defe92b", derivation.EntropyHex);
        Assert.Equal(
            "diet glad hat rural panther lawsuit act drop gallery urge where fit",
            string.Join(' ', derivation.Words));
    }

    /// <summary>
    /// There is no separator option, and this test records why, because "add a dialect
    /// selector" is the obvious thing for a future reader to want.
    ///
    /// Krux joins d6 rolls with nothing and reserves "-" for d20, where a face value can run
    /// to two digits and "1" then "2" would otherwise be indistinguishable from "12":
    ///
    ///     "".join(self.rolls) if self.num_sides &lt; 10 else "-".join(self.rolls)
    ///         -- src/krux/pages/new_mnemonic/dice_rolls.py, unchanged since v22.08.2
    ///
    /// So for d6 there is no dialect split to support: Coldcard, SeedSigner and Krux agree.
    /// A dash separator in a d6-only app would produce a seed that no vendor reproduces, which
    /// is the exact failure this app exists to detect. Vectors 5 and 9 come from two different
    /// vendors and agree, which is the positive form of the same statement.
    /// </summary>
    [Fact]
    public void The_preimage_carries_no_separator_because_no_d6_vendor_uses_one()
    {
        var derivation = DiceSeed.Derive(Rolls50, WordCount.Twelve).Value;

        Assert.DoesNotContain("-", derivation.Preimage, StringComparison.Ordinal);
        Assert.Equal(50, derivation.Preimage.Length);
    }

    /// <summary>Vector 7: the primary case. 50 rolls, 12 words, minimum enforced.</summary>
    [Fact]
    public void Vector_7_fifty_rolls_at_12_words()
    {
        var derivation = DiceSeed.Derive(Rolls50, WordCount.Twelve).Value;

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
        var derivation = DiceSeed.Derive(Rolls99, WordCount.TwentyFour).Value;

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
        var derivation = DiceSeed.Derive(Rolls50 + "\n", WordCount.Twelve).Value;

        Assert.Equal(Rolls50, derivation.Preimage);
        Assert.Equal("ee72ae915a4e6ea7ccbeb8e5e5eecef29a1d0d90f053183726a424b6d3b07325", derivation.Sha256Hex);
    }

    [Fact]
    public void The_public_entry_point_enforces_the_minimum()
    {
        var result = DiceSeed.Derive("123456", WordCount.Twelve);

        Assert.True(result.IsFailure);
        Assert.Contains("50", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bad_character_fails_before_anything_is_hashed()
    {
        var result = DiceSeed.Derive(Rolls50.Remove(10, 1).Insert(10, "7"), WordCount.Twelve);

        Assert.True(result.IsFailure);
        Assert.Contains("'7'", result.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(WordCount.Twelve, 12, 32)]
    [InlineData(WordCount.TwentyFour, 24, 64)]
    public void Word_count_selects_the_entropy_length(WordCount words, int expectedWords, int expectedEntropyHexLength)
    {
        var derivation = DiceSeed.Derive(Rolls99, words).Value;

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
        var derivation = DiceSeed.Derive(Rolls99, words).Value;

        Assert.StartsWith(derivation.EntropyHex, derivation.Sha256Hex, StringComparison.Ordinal);
    }
}
