using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The backup key mode: rolls in, a 32-byte key and a check code out, no words anywhere.
///
/// Every value asserted here was recomputed with sha256sum against the exact strings below
/// before being written down, using the same two commands the UI tells the user to run:
///
///   printf '%s' "$ROLLS" | sha256sum
///   printf '%s' "$K_HEX" | sha256sum | cut -c1-4
/// </summary>
public class BackupKeyTests
{
    // The 50 and 99 roll logs from SeedSigner's published examples, reused here because they
    // contain 6s and are the logs already verified elsewhere in this suite.
    const string Rolls50 = "65515223131652132161133154444123616466443112153441";
    const string Rolls99 = "655152231316521321611331544441236164664431121534415633526456254462245546236542364246312613322234612";

    [Fact]
    public void Fifty_rolls_produce_the_published_hash_as_the_key()
    {
        var derivation = BackupKey.Derive(Rolls50, EntropyStrength.Bits128).Value;

        Assert.Equal("6cb09af855050dcde6fe2adc3181c250982011e2cf17821cbed56a908ec527c3", derivation.KeyHex);
        Assert.Equal("6ca2", derivation.CheckCode);
        Assert.Equal(Rolls50, derivation.Preimage);
    }

    [Fact]
    public void Ninety_nine_rolls_produce_the_published_hash_as_the_key()
    {
        var derivation = BackupKey.Derive(Rolls99, EntropyStrength.Bits256).Value;

        Assert.Equal("51531761ec7a738946e0b9f46bb11320a695495430e345c14f01ad8b3b898a6d", derivation.KeyHex);
        Assert.Equal("a32a", derivation.CheckCode);
    }

    /// <summary>
    /// The key is 32 bytes whatever the roll count. This is the property that makes a short log
    /// dangerous rather than merely weak: nothing about the output announces it.
    /// </summary>
    [Theory]
    [InlineData(EntropyStrength.Bits128, Rolls50)]
    [InlineData(EntropyStrength.Bits256, Rolls99)]
    public void The_key_is_always_thirty_two_bytes(EntropyStrength strength, string rolls) =>
        Assert.Equal(64, BackupKey.Derive(rolls, strength).Value.KeyHex.Length);

    /// <summary>
    /// The reason the two roll logs must never be the same one, stated as a test rather than
    /// only as a comment.
    ///
    /// On 24 words the BIP-39 entropy IS this key, byte for byte. On 12 words it is its first
    /// half. So a user who rolls once and derives both gets a backup key that is the wallet it
    /// is supposed to be protecting, and the shares protect nothing: anyone who reconstructs k
    /// reads the seed straight out of it.
    ///
    /// If this test ever fails, the derivation changed and the warnings built on it need
    /// revisiting. It is not asserting desirable behaviour; it is pinning the hazard.
    /// </summary>
    [Fact]
    public void A_reused_roll_log_makes_the_key_identical_to_the_seed_entropy()
    {
        var key = BackupKey.Derive(Rolls99, EntropyStrength.Bits256).Value.KeyHex;

        var twentyFour = DiceSeed.Derive(Rolls99, WordCount.TwentyFour).Value.EntropyHex;
        var twelve = DiceSeed.Derive(Rolls99, WordCount.Twelve).Value.EntropyHex;

        Assert.Equal(key, twentyFour);
        Assert.Equal(key[..32], twelve);
    }

    /// <summary>
    /// The check code is computed over the printed hex string, not the key bytes, so that it
    /// can be reproduced from what is on screen with no decoding step.
    /// </summary>
    [Fact]
    public void The_check_code_is_taken_from_the_hash_of_the_printed_hex()
    {
        const string keyHex = "6cb09af855050dcde6fe2adc3181c250982011e2cf17821cbed56a908ec527c3";

        // printf '%s' "$K_HEX" | sha256sum | cut -c1-4
        Assert.Equal("6ca2", BackupKey.CheckCodeFor(keyHex));
        Assert.Equal(BackupKey.CheckCodeLength, BackupKey.CheckCodeFor(keyHex).Length);
    }

    /// <summary>
    /// A single wrong character has to be caught, because that is the only failure this code
    /// exists for. Sixteen bits means a slip gets through about once in 65,536 times, which is
    /// the accepted limit of a four-character code.
    /// </summary>
    [Fact]
    public void A_mistyped_key_does_not_keep_its_check_code()
    {
        const string keyHex = "6cb09af855050dcde6fe2adc3181c250982011e2cf17821cbed56a908ec527c3";
        const string mistyped = "6cb09af855050dcde6fe2adc3181c250982011e2cf17821cbed56a908ec527c4";

        Assert.NotEqual(BackupKey.CheckCodeFor(keyHex), BackupKey.CheckCodeFor(mistyped));
    }

    [Fact]
    public void The_key_is_grouped_into_sixteen_blocks_of_four_for_transcription()
    {
        var groups = BackupKey.Derive(Rolls50, EntropyStrength.Bits128).Value.KeyHexGroups;

        Assert.Equal(16, groups.Count);
        Assert.All(groups, group => Assert.Equal(4, group.Length));
        Assert.Equal("6cb0", groups[0]);
    }

    [Theory]
    [InlineData(EntropyStrength.Bits128, 49)]
    [InlineData(EntropyStrength.Bits256, 98)]
    public void A_log_below_the_minimum_is_refused(EntropyStrength strength, int rollCount)
    {
        var result = BackupKey.Derive(new string('4', rollCount), strength);

        Assert.True(result.IsFailure);
        Assert.Contains($"{rollCount} rolls is below", result.Error);
    }

    /// <summary>
    /// The minimum is the same rule the seed mode applies for the same entropy target, which is
    /// the point of stating it against bits rather than against words.
    /// </summary>
    [Theory]
    [InlineData(WordCount.Twelve, EntropyStrength.Bits128)]
    [InlineData(WordCount.TwentyFour, EntropyStrength.Bits256)]
    public void The_minimum_matches_the_seed_mode_for_the_same_strength(WordCount words, EntropyStrength strength) =>
        Assert.Equal(DiceRolls.MinimumRollsFor(words), DiceRolls.MinimumRollsFor(strength));

    [Fact]
    public void A_character_that_is_not_a_die_face_is_refused()
    {
        var result = BackupKey.Derive(Rolls50.Remove(10, 1).Insert(10, "7"), EntropyStrength.Bits128);

        Assert.True(result.IsFailure);
        Assert.Contains("position 11", result.Error);
    }
}
