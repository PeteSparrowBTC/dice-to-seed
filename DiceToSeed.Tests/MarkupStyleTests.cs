using System.Text.RegularExpressions;

namespace DiceToSeed.Tests;

/// <summary>
/// index.html and app.css have to agree, and until 1.3.2 they did not.
///
/// The defect: index.html carried the Blazor template's loading indicator, two SVG circles with
/// class "loading-progress", while this app replaced the template's stylesheet wholesale instead
/// of editing it. The rules that size and stroke those circles live in the template's stylesheet,
/// so the markup arrived with none: SVG fills black by default and an svg with no width collapses,
/// which put a black lump and an empty caption on screen for as long as the runtime took to
/// download. It shipped in every release up to and including 1.3.1, on the demo and in the
/// AppImage, and the compiler cannot see it because markup and stylesheets are copied rather than
/// checked against each other.
///
/// So the check is mechanical: every class index.html uses must have at least one rule. That is
/// the exact shape of the bug, and it also caught the reload link in the error strip, which was
/// taking the browser's default link colour on a red background.
///
/// The boot screen is worth having rather than deleting. The WebAssembly runtime is several
/// megabytes, and a window that stays blank for a few seconds looks like a program that failed to
/// start, which in a tool that derives keys invites someone to reload halfway through.
/// </summary>
public class MarkupStyleTests
{
    /// <summary>
    /// The one that would have caught the lump. A class in the markup with no rule anywhere in
    /// the stylesheet renders at browser defaults, which for the template's SVG meant a black
    /// blob and for a link meant unreadable contrast.
    /// </summary>
    [Fact]
    public void Every_class_used_in_index_html_has_a_rule_in_the_stylesheet()
    {
        var stylesheet = ReadWebFile("wwwroot/css/app.css");

        var unstyled = ClassesUsedInMarkup()
            .Where(name => !Regex.IsMatch(stylesheet, $@"(?<![-\w])\.{Regex.Escape(name)}(?![-\w])"))
            .ToList();

        Assert.True(unstyled.Count == 0,
            "These classes appear in index.html and have no rule in app.css, so they render at " +
            "browser defaults. This is how the template's loading ring shipped as an unstyled " +
            "lump up to 1.3.1: " + string.Join(", ", unstyled));
    }

    /// <summary>
    /// A guard nobody has seen fail is not known to work. Proves the matcher would reject the
    /// markup that was actually shipping, without needing anyone to paste it back in.
    /// </summary>
    [Fact]
    public void The_matcher_would_have_rejected_the_template_loading_ring()
    {
        var stylesheet = ReadWebFile("wwwroot/css/app.css");

        Assert.False(Regex.IsMatch(stylesheet, @"(?<![-\w])\.loading-progress(?![-\w])"),
            "app.css now has a rule for the template's loading ring, which means the check below proves nothing.");

        // On the classes the markup uses, not on the file's text: index.html names the old class
        // in a comment explaining why it went, exactly as the entropy guard excludes its own file
        // for containing every string it searches for.
        Assert.DoesNotContain("loading-progress", ClassesUsedInMarkup());
    }

    /// <summary>
    /// The bar has to be driven by the runtime's own progress rather than animated, so a load that
    /// stalls looks stalled. Blazor sets both of these on the document element as each file lands.
    /// </summary>
    [Fact]
    public void The_boot_bar_reports_real_progress()
    {
        var stylesheet = ReadWebFile("wwwroot/css/app.css");

        Assert.Contains("--blazor-load-percentage", stylesheet, StringComparison.Ordinal);
        Assert.Contains("--blazor-load-percentage-text", stylesheet, StringComparison.Ordinal);

        // Both read with a fallback, because neither variable exists until the first file
        // completes and a bar with no width at all would read as a missing element.
        Assert.Contains("var(--blazor-load-percentage, 0%)", stylesheet, StringComparison.Ordinal);
    }

    /// <summary>
    /// The boot screen is markup this repository owns. If it is ever removed, the window is blank
    /// while several megabytes download, so this pins its presence rather than its wording.
    /// </summary>
    [Fact]
    public void The_boot_screen_is_present_inside_the_app_element()
    {
        var markup = ReadWebFile("wwwroot/index.html");

        var app = Regex.Match(markup, @"<div id=""app"">(?<body>.*?)</div>\s*</div>", RegexOptions.Singleline);

        Assert.True(app.Success, "Could not find the #app element in index.html.");
        Assert.Contains("boot-bar", app.Groups["body"].Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// The scan is worthless if it reads nothing, which is what happens when a project is renamed.
    /// </summary>
    [Fact]
    public void The_scan_actually_reaches_the_markup()
    {
        var classes = ClassesUsedInMarkup().ToList();

        Assert.True(classes.Count >= 3, $"Expected several classes in index.html, found {classes.Count}.");
        Assert.Contains("boot", classes);
    }

    /// <summary>
    /// Anchors have to be styled, and this is not hypothetical: the app had no links at all until the
    /// roll sheet needed one, so there was no rule for them, and the first one rendered in the
    /// browser's default rgb(0, 0, 238) on a near-black page. Unreadable, and invisible to the class
    /// check above, because an element selector is not a class.
    ///
    /// Third instance of one shape of bug: markup whose styling was assumed rather than written. The
    /// loading ring and the error strip's reload link were the other two.
    /// </summary>
    [Fact]
    public void Anchors_are_styled()
    {
        var stylesheet = ReadWebFile("wwwroot/css/app.css");

        Assert.Matches(@"(?m)^a\s*\{", stylesheet);
        Assert.Matches(@"(?m)^a\s*\{[^}]*color:", stylesheet);
    }

    static IEnumerable<string> ClassesUsedInMarkup() =>
        Regex.Matches(ReadWebFile("wwwroot/index.html"), @"class=""(?<names>[^""]+)""")
            .SelectMany(match => match.Groups["names"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal);

    static string ReadWebFile(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "DiceToSeed.Web", relativePath));

    /// <summary>
    /// Walks up from the test binary to the directory holding the solution, so the scan does not
    /// depend on the depth of the build output path. Same approach as the entropy guard.
    /// </summary>
    static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("DiceToSeed.slnx").Any())
            directory = directory.Parent;

        return directory ?? throw new InvalidOperationException(
            $"Could not find DiceToSeed.slnx above {AppContext.BaseDirectory}.");
    }
}
