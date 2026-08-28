using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The rows the page shows for checking a recorded log against a handwritten sheet, and the printed
/// sheets themselves.
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
    /// There are two printed sheets, one per word count, and both must number their rows the way the
    /// app does, or the comparison they exist for becomes an exercise in counting, which is the
    /// mistake being hunted.
    ///
    /// Read out of the served HTML and checked against the arithmetic the page uses. Change the
    /// grouping to fives and this fails, which is the point: paper already in a drawer cannot be
    /// reprinted when the code changes.
    /// </summary>
    [Theory]
    [InlineData("roll-sheet-12-words.html", 60)]
    [InlineData("roll-sheet-24-words.html", 111)]
    public void Each_printed_sheet_numbers_its_rows_the_way_the_app_does(string sheet, int rollCount)
    {
        var printed = Regex.Matches(Sheet(sheet), @"<td class=""n"">(?<first>\d+)</td>")
            .Select(match => int.Parse(match.Groups["first"].Value))
            .ToList();

        var onScreen = Log(new string('1', rollCount)).RowsOfTen.Select(row => row.FirstRoll).ToList();

        Assert.Equal(onScreen, printed);
    }

    /// <summary>
    /// A box for every roll of the count the sheet is for, and none spare that could be filled in by
    /// mistake. The 24-word sheet ends nine boxes into a row, and those are greyed rather than absent
    /// so the grid stays rectangular; they must not be usable.
    /// </summary>
    [Theory]
    [InlineData("roll-sheet-12-words.html", 60, 0)]
    [InlineData("roll-sheet-24-words.html", 111, 9)]
    public void Each_printed_sheet_has_a_box_per_roll(string sheet, int rollCount, int greyed)
    {
        var cells = Regex.Matches(Sheet(sheet), @"<td class=""(?<classes>box[^""]*)""")
            .Select(match => match.Groups["classes"].Value)
            .ToList();

        Assert.Equal(rollCount, cells.Count(c => !c.Contains("unused")));
        Assert.Equal(greyed, cells.Count(c => c.Contains("unused")));
    }

    /// <summary>
    /// Both are paper: they have to print the same everywhere, which means no script deciding what is
    /// on them, and they must not reach for anything off the machine. And both must carry the two
    /// lines that make writing the rolls down safe to ask for at all.
    /// </summary>
    [Theory]
    [InlineData("roll-sheet-12-words.html")]
    [InlineData("roll-sheet-24-words.html")]
    public void Each_printed_sheet_needs_no_script_and_no_network(string sheet)
    {
        var markup = Sheet(sheet);

        Assert.DoesNotContain("<script", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"https?://", markup);
        Assert.Contains("Destroy this", markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a backup", markup, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The page links the sheet for the word count in effect, with relative hrefs.
    ///
    /// Relative matters twice: it resolves against the base tag, so one href works at "/" in the
    /// AppImage and "/dice-to-seed/" on Pages, and an absolute one would be an external reference in
    /// an app that must load with the network disconnected.
    /// </summary>
    [Fact]
    public void The_page_links_the_sheet_for_the_selected_word_count()
    {
        var page = File.ReadAllText(Path.Combine(
            RepositoryRoot().FullName, "DiceToSeed.Web", "Pages", "Derive.razor"));

        Assert.Contains(@"href=""@(SheetBaseName).html""", page, StringComparison.Ordinal);
        Assert.Contains(@"href=""@(SheetBaseName).pdf""", page, StringComparison.Ordinal);
        Assert.Contains("roll-sheet-12-words", page, StringComparison.Ordinal);
        Assert.Contains("roll-sheet-24-words", page, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"href=""https?://[^""]*roll-sheet", page);
    }

    /// <summary>
    /// Both sheets live where the build publishes them, or the links are a 404 in the AppImage and on
    /// the demo alike.
    /// </summary>
    [Theory]
    [InlineData("roll-sheet-12-words.html")]
    [InlineData("roll-sheet-12-words.pdf")]
    [InlineData("roll-sheet-24-words.html")]
    [InlineData("roll-sheet-24-words.pdf")]
    public void Each_sheet_file_lives_in_the_published_folder(string file) =>
        Assert.True(File.Exists(Path.Combine(
            RepositoryRoot().FullName, "DiceToSeed.Web", "wwwroot", file)), file + " is missing");

    /// <summary>
    /// Each PDF is a committed binary generated from its HTML, which buys a file anybody can open and
    /// costs the one thing a derived artifact always costs: it can go stale unnoticed. Here that is
    /// precisely the failure the sheet exists to prevent, a printed page quietly disagreeing with the
    /// screen it is meant to be checked against.
    ///
    /// Line endings are normalised before hashing. A raw hash of the file is a hash of the checkout as
    /// much as of the sheet: git writes CRLF into a Windows working tree and LF into a Linux one, so
    /// the first version of this guard passed locally and failed on the runner.
    /// </summary>
    [Theory]
    [InlineData("roll-sheet-12-words")]
    [InlineData("roll-sheet-24-words")]
    public void Each_pdf_is_not_stale(string name)
    {
        var recorded = File.ReadAllText(Path.Combine(
            RepositoryRoot().FullName, "DiceToSeed.Web", "wwwroot", name + ".pdf.source-sha256")).Trim();

        var actual = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(Sheet(name + ".html").Replace("\r\n", "\n"))));

        Assert.True(recorded == actual,
            $"{name}.html has changed and {name}.pdf was not regenerated, so the printed sheet and the " +
            $"screen can now disagree. Regenerate it and update {name}.pdf.source-sha256. The command is " +
            $"in the comment at the top of the sheet. Recorded {recorded}, found {actual}.");
    }

    /// <summary>
    /// And that each is a PDF of one page on A4. One page is the point of the layout work: a grid split
    /// across a break is useless, and a second sheet of empty boxes is somebody wondering what they
    /// missed.
    /// </summary>
    [Theory]
    [InlineData("roll-sheet-12-words.pdf")]
    [InlineData("roll-sheet-24-words.pdf")]
    public void Each_pdf_is_a_single_a4_page(string file)
    {
        var pdf = File.ReadAllBytes(Path.Combine(
            RepositoryRoot().FullName, "DiceToSeed.Web", "wwwroot", file));

        Assert.StartsWith("%PDF-", Encoding.Latin1.GetString(pdf, 0, 5));

        var text = Encoding.Latin1.GetString(pdf);

        var count = Regex.Match(text, @"/Type\s*/Pages.*?/Count\s+(?<n>\d+)", RegexOptions.Singleline);
        Assert.True(count.Success, "Could not find the page tree in " + file);
        Assert.Equal("1", count.Groups["n"].Value);

        // A4 is 595 x 842 points, checked loosely: the exact value carries renderer rounding, and what
        // is worth catching is Letter or a custom size, not a fraction of a point.
        var box = Regex.Match(text, @"/MediaBox\s*\[\s*0\s+0\s+(?<w>[\d.]+)\s+(?<h>[\d.]+)");
        Assert.True(box.Success, "Could not find the page size in " + file);
        Assert.InRange(double.Parse(box.Groups["w"].Value), 594, 597);
        Assert.InRange(double.Parse(box.Groups["h"].Value), 841, 844);
    }

    static string Sheet(string file) =>
        File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "DiceToSeed.Web", "wwwroot", file));

    static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("DiceToSeed.slnx").Any())
            directory = directory.Parent;

        return directory ?? throw new InvalidOperationException(
            $"Could not find DiceToSeed.slnx above {AppContext.BaseDirectory}.");
    }
}
