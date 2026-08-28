using System.Text.RegularExpressions;

namespace DiceToSeed.Tests;

/// <summary>
/// The keys 1 to 6 record a roll and Backspace undoes one, and up to 1.4.3 none of them did
/// anything until the user had clicked the dice pad with the mouse.
///
/// The defect: the handler was bound to the pad. A key event goes to the focused element and
/// bubbles upward, so a handler sees only keys pressed while focus is inside the element carrying
/// it. Nothing was focused on load, the pad's autofocus attribute was ignored (it is honoured while
/// the browser parses the document, and a WebAssembly page is rendered after the parse), so the
/// first keystroke went to the document body and vanished. The way to make the keys work was to use
/// the mouse, which is what they exist to avoid, and on a page where fifty to a hundred and eleven
/// presses are the entire job.
///
/// It is the same shape as the loading ring and the first anchor: markup whose behaviour was
/// assumed rather than exercised. The compiler cannot see it, the derivation vectors cannot see it,
/// and the page renders perfectly while doing nothing.
///
/// So this pins the two facts the fix rests on, against the markup rather than against the prose
/// describing it: the handler is on the element that wraps the page, and something focuses that
/// element on the first render.
/// </summary>
public class KeyboardShortcutTests
{
    /// <summary>
    /// Bound to the wrapper, so a key pressed with focus on any control inside the page reaches it:
    /// a die button, a mode radio, an open disclosure, Derive.
    /// </summary>
    [Fact]
    public void The_key_handler_is_bound_to_the_element_that_wraps_the_page()
    {
        var wrapper = OpeningTag("div", "app");

        Assert.Contains("@onkeydown=\"HandleKey\"", wrapper, StringComparison.Ordinal);
    }

    /// <summary>
    /// The other half, and the part a reader is most likely to delete as noise. Clicking a paragraph
    /// or the page background focuses nothing, which blurs to the document body: that is above the
    /// wrapper rather than inside it, so the keys would go quiet exactly as they did before. With
    /// the attribute the browser focuses the nearest focusable ancestor, which is the wrapper.
    ///
    /// -1 rather than 0 because the wrapper must not become a tab stop of its own.
    /// </summary>
    [Fact]
    public void The_wrapper_is_focusable_without_joining_the_tab_order()
    {
        var wrapper = OpeningTag("div", "app");

        Assert.Contains("tabindex=\"-1\"", wrapper, StringComparison.Ordinal);
    }

    /// <summary>
    /// Focus has to land there on load, or the first keystroke of a session is still lost. Matched
    /// on the call rather than on the method name: the comment above it names the method, and a
    /// guard that its own documentation satisfies is worse than no guard. That has happened here
    /// three times.
    /// </summary>
    [Fact]
    public void Focus_is_placed_on_the_wrapper_on_the_first_render()
    {
        var markup = DerivePage();

        Assert.Matches(@"keyboardScope\.FocusAsync\(", markup);
        Assert.Matches(@"if\s*\(firstRender\)", markup);
    }

    /// <summary>
    /// Where the handler must not go back to. Keeping the pad a tab stop is wanted, so the assertion
    /// is specifically about the handler, not about the attributes.
    /// </summary>
    [Fact]
    public void The_dice_pad_carries_no_handler_of_its_own()
    {
        var pad = OpeningTag("div", "dice");

        Assert.DoesNotContain("@onkeydown", pad, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"0\"", pad, StringComparison.Ordinal);
    }

    /// <summary>
    /// The one control on the page that a digit must not reach. The mode-switch confirmation is a
    /// question about discarding the log, and its buttons are inside the wrapper, so without this
    /// the shortcuts would append to a log the user is being asked about.
    /// </summary>
    [Fact]
    public void The_shortcuts_are_inert_while_the_mode_switch_is_waiting_on_an_answer()
    {
        var markup = DerivePage();

        var handler = Regex.Match(markup, @"HandleKey\(KeyboardEventArgs[^)]*\)\s*\{(?<body>.*?)\n    \}",
            RegexOptions.Singleline);

        Assert.True(handler.Success, "Could not find HandleKey in Derive.razor.");
        Assert.Matches(@"pendingMode is (not null|\{ \})[\s\S]*?return;", handler.Groups["body"].Value);
    }

    /// <summary>
    /// The same silence by a second route, found by clicking through the running app rather than by
    /// reading the code. A control that disappears or goes disabled while it holds focus does not
    /// pass focus on: the browser drops it to the document body, which is outside the wrapper, and
    /// the shortcuts stop working again. Three paths on this page do that, and each one calls the
    /// wrapper back:
    ///
    /// - either answer to the mode-switch confirmation removes the button that was clicked,
    /// - undoing the last roll disables Undo and Clear.
    ///
    /// Matched on the call, since the name of the method appears in prose above each of them.
    /// </summary>
    [Fact]
    public void Focus_returns_to_the_wrapper_wherever_a_render_takes_it_away()
    {
        var markup = DerivePage();

        var calls = Regex.Matches(markup, @"await RestoreKeyboardFocus\(\);").Count;

        Assert.True(calls >= 4,
            $"Expected the wrapper to be refocused on every path that can drop focus to the body, found {calls} calls.");

        // The definition, so the count above cannot be satisfied by calls to something that no
        // longer moves focus.
        Assert.Matches(@"RestoreKeyboardFocus\(\)\s*=>\s*keyboardScope\.FocusAsync\(", markup);
    }

    /// <summary>
    /// A scan that matches nothing asserts nothing. Both tags above are found by the same helper, so
    /// one check covers the pair.
    /// </summary>
    [Fact]
    public void The_scan_actually_reaches_both_elements()
    {
        Assert.Contains("class=\"app", OpeningTag("div", "app"), StringComparison.Ordinal);
        Assert.Contains("class=\"dice\"", OpeningTag("div", "dice"), StringComparison.Ordinal);
    }

    /// <summary>
    /// The opening tag of the first element with this class, from the '&lt;' to the '&gt;' that closes
    /// the tag. Reading the tag rather than the file is what keeps these assertions off the comments,
    /// which necessarily quote every attribute they explain.
    /// </summary>
    static string OpeningTag(string element, string className)
    {
        // The class name has to end where the attribute says it does, or "dice" matches the
        // "dice-actions" row of buttons that happens to appear earlier in the file.
        var match = Regex.Match(DerivePage(), $@"<{element} class=""{className}(?:""|\s)[^>]*>");

        Assert.True(match.Success, $"Could not find a <{element}> with class \"{className}\" in Derive.razor.");

        return match.Value;
    }

    static string DerivePage() =>
        File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "DiceToSeed.Web", "Pages", "Derive.razor"));

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
