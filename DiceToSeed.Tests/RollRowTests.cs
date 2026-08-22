using System.Text.RegularExpressions;
using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The rows the page shows for checking a recorded log against a handwritten sheet.
///
/// Why this matters more than a display detail: a mis-press is the one error no other check in this
/// app can find. Every verification it offers compares the log against another implementation of the
/// conversion, so two tools handed the same wrong log agree perfectly, the counter reads the full
/// count, and the words are valid BIP-39 for a wallet the dice never made. Comparing against a paper
/// note is the only defence, and comparing sixty undifferentiated digits is how you lose your place.
/// </summary>
public class RollRowTests
{
    static DiceRollLog Log(string digits) => DiceRolls.Read(digits).Value;

    /// <summary>
    /// The rows have to reassemble into exactly the log, or the thing being checked is not the thing
    /// being hashed. Checked across every length from one to a hundred and twenty, which covers both
    /// recommended counts and every partial final row.
    /// </summary>
    [Fact]
    public void The_rows_always_reassemble_into_the_preimage()
    {
        var mismatched = from length in Enumerable.Range(1, 120)
                         let digits = string.Concat(Enumerable.Range(0, length).Select(i => (char)('1' + i % 6)))
                         let log = Log(digits)
                         let rejoined = string.Concat(log.RowsOfTen.Select(row => row.Digits))
                         where rejoined != log.Preimage
                         select length;

        Assert.Empty(mismatched);
    }

    /// <summary>
    /// Every row is ten long except possibly the last, and the numbering is the position of the
    /// first roll in the row: 1, 11, 21. If the numbering slips, the aid points at the wrong place
    /// and is worse than no aid.
    /// </summary>
    [Fact]
    public void Rows_are_tens_numbered_by_first_roll()
    {
        var rows = Log(new string('1', 60)).RowsOfTen;

        Assert.Equal(6, rows.Count);
        Assert.Equal([1, 11, 21, 31, 41, 51], rows.Select(row => row.FirstRoll));
        Assert.All(rows, row => Assert.Equal(10, row.Digits.Length));
    }

    /// <summary>
    /// The recommended 24-word length is not a multiple of ten, so the short final row is the normal
    /// case rather than an edge case.
    /// </summary>
    [Fact]
    public void A_short_final_row_is_returned_as_it_is()
    {
        var rows = Log(new string('6', 111)).RowsOfTen;

        Assert.Equal(12, rows.Count);

        // Eleven full rows cover rolls 1 to 110, so the twelfth begins at 111 and holds one digit.
        Assert.Equal(101, rows[^2].FirstRoll);
        Assert.Equal(10, rows[^2].Digits.Length);
        Assert.Equal(111, rows[^1].FirstRoll);
        Assert.Equal("6", rows[^1].Digits);
        Assert.Equal(111, rows.Sum(row => row.Digits.Length));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 1)]
    [InlineData(11, 2)]
    [InlineData(50, 5)]
    [InlineData(99, 10)]
    public void The_row_count_is_the_length_rounded_up(int rollCount, int expectedRows) =>
        Assert.Equal(expectedRows, Log(new string('3', rollCount)).RowsOfTen.Count);

    /// <summary>
    /// The printed sheet and the screen must number their rows identically, or the comparison the
    /// sheet exists for becomes an exercise in counting, which is the mistake being hunted.
    ///
    /// So the sheet's row labels are read out of printable/roll-sheet.html and checked against the
    /// arithmetic the page uses. Change the grouping to fives and this fails, which is the point: the
    /// paper cannot be reprinted by everyone who already has a copy in a drawer.
    /// </summary>
    [Fact]
    public void The_printed_sheet_numbers_its_rows_the_way_the_app_does()
    {
        var sheet = Sheet();

        var printed = Regex.Matches(sheet, @"<td class=""n"">(?<first>\d+)</td>")
            .Select(match => int.Parse(match.Groups["first"].Value))
            .ToList();

        var onScreen = Log(new string('1', 111)).RowsOfTen.Select(row => row.FirstRoll).ToList();

        Assert.Equal(onScreen, printed);

        // And a box for every roll of the longest recommended log, or somebody runs out of paper at
        // roll 101 with the ceremony half done.
        Assert.Equal(111, Regex.Matches(sheet, @"<td class=""box""(>| )").Count);
    }

    /// <summary>
    /// It is paper. It has to print the same everywhere, which means no script deciding what is on
    /// it, and it must not reach for anything off the machine.
    /// </summary>
    [Fact]
    public void The_printed_sheet_needs_no_script_and_no_network()
    {
        var sheet = Sheet();

        Assert.DoesNotContain("<script", sheet, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"https?://", sheet);

        // The two lines that make writing the rolls down safe to ask for in the first place.
        Assert.Contains("Destroy this", sheet, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a backup", sheet, StringComparison.OrdinalIgnoreCase);
    }

    static string Sheet() =>
        File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "printable", "roll-sheet.html"));

    static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("DiceToSeed.slnx").Any())
            directory = directory.Parent;

        return directory ?? throw new InvalidOperationException(
            $"Could not find DiceToSeed.slnx above {AppContext.BaseDirectory}.");
    }
}
