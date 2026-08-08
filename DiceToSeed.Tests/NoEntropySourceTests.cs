using System.Text.RegularExpressions;

namespace DiceToSeed.Tests;

/// <summary>
/// The rule this enforces: this app is never a source of entropy. It converts entropy the
/// user produced on physical dice, and it has no random number generator of any kind. Not a
/// "roll for me" button, not a simulated die, not a developer convenience that fills the box
/// with random digits, not a nonce, not an id.
///
/// Why a test rather than a note in the README: the name "dice-to-seed" invites the feature,
/// and the feature turns an audit tool into a browser-based key generator, which is the worst
/// available place to make a key. A rule that only lives in prose gets traded away by someone
/// in a hurry. See CLAUDE.md rule 1 and section 3a of the plan.
///
/// Two scoping decisions matter, and getting either wrong produces a test that is useless:
///
///   1. This scans FIRST-PARTY SOURCE ONLY. It must never be pointed at published output.
///      The .NET WebAssembly runtime calls crypto.getRandomValues on its own account, so a
///      scan over publish/wwwroot would fail forever for a reason that is not a defect in
///      this repository, and a permanently red check gets deleted rather than fixed.
///   2. This file is excluded from its own scan. It necessarily contains every string it
///      searches for.
/// </summary>
public class NoEntropySourceTests
{
    // Each entry is a way to obtain a value the user did not roll. The list is deliberately
    // broader than "an RNG": Guid.NewGuid and a timestamp are both entropy sources when
    // someone reaches for "something unique" while wiring up a UI.
    static readonly IReadOnlyList<string> ForbiddenTokens =
    [
        "RandomNumberGenerator",
        "System.Random",
        "new Random",
        "Random.Shared",
        "Guid.NewGuid",
        "crypto.getRandomValues",
        "Math.random",
        "GetNonZeroBytes",
        "RNGCryptoServiceProvider",
    ];

    // Extensions worth reading. Anything else in wwwroot is an asset, not code.
    static readonly IReadOnlyList<string> ScannedExtensions = [".cs", ".razor", ".js", ".css", ".html"];

    // bin and obj hold build output, including copies of this repository's own source and of
    // the framework. wwwroot/lib would hold third-party assets if any were ever added; the
    // Blazor template's bootstrap bundle (which does call Math.random) was deleted rather
    // than excluded, and this entry keeps a future re-add from being scanned by accident
    // instead of being questioned.
    static readonly IReadOnlyList<string> ExcludedDirectorySegments = ["bin", "obj", "wwwroot/lib", "wwwroot/_framework"];

    [Fact]
    public void No_first_party_source_file_contains_a_source_of_entropy()
    {
        var offenders = FirstPartySourceFiles()
            .SelectMany(file => ForbiddenTokens
                .Where(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{Relative(file)} contains \"{token}\""))
            .ToList();

        Assert.True(offenders.Count == 0,
            "This app must never generate entropy; it converts dice rolls the user brought. " +
            "See CLAUDE.md rule 1. Offending occurrences:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// The scan is worthless if it silently reads nothing, which is exactly what happens when
    /// the solution is moved or a project is renamed. Assert that it found real files.
    /// </summary>
    [Fact]
    public void The_scan_actually_reaches_the_source()
    {
        var files = FirstPartySourceFiles().ToList();

        Assert.True(files.Count >= 5, $"Expected to scan several source files, found {files.Count}. " +
                                      $"Repository root resolved to {RepositoryRoot().FullName}.");
        Assert.Contains(files, f => f.EndsWith(".razor", StringComparison.Ordinal));
        Assert.Contains(files, f => f.EndsWith(".cs", StringComparison.Ordinal));
    }

    /// <summary>
    /// A guard that has never failed is not known to work. This proves the matcher itself
    /// fires, without needing anyone to temporarily paste an RNG into Core and remember to
    /// take it out again.
    /// </summary>
    [Theory]
    [InlineData("var bytes = RandomNumberGenerator.GetBytes(16);")]
    [InlineData("var d = new Random().Next(1, 7);")]
    [InlineData("const rolls = crypto.getRandomValues(new Uint8Array(50));")]
    [InlineData("id = Guid.NewGuid().ToString();")]
    public void The_matcher_fires_on_a_known_bad_line(string line) =>
        Assert.Contains(ForbiddenTokens, token => line.Contains(token, StringComparison.Ordinal));

    /// <summary>
    /// No external reference in first-party source: no CDN, no web font, no analytics, no
    /// outbound link. Loopback addresses are allowed because the verification panel tells the
    /// user to serve the app on 127.0.0.1, and that instruction is the point of the panel.
    /// </summary>
    [Fact]
    public void No_first_party_source_file_references_an_external_origin()
    {
        var externalUrl = new Regex(@"https?://(?!127\.0\.0\.1|localhost)", RegexOptions.IgnoreCase);

        var offenders = FirstPartySourceFiles()
            .SelectMany(file => File.ReadLines(file)
                .Select((text, index) => (text, number: index + 1))
                .Where(line => externalUrl.IsMatch(line.text))
                .Select(line => $"{Relative(file)}:{line.number}: {line.text.Trim()}"))
            .ToList();

        Assert.True(offenders.Count == 0,
            "The published app must load with the network disconnected, so no first-party file may " +
            "reference an external origin. See CLAUDE.md rule 6. Offending lines:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    static IEnumerable<string> FirstPartySourceFiles() =>
        new[] { "DiceToSeed.Core", "DiceToSeed.Web" }
            .Select(project => Path.Combine(RepositoryRoot().FullName, project))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            .Where(file => ScannedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Where(IsNotExcluded);

    static bool IsNotExcluded(string file)
    {
        var normalised = file.Replace('\\', '/');

        return !ExcludedDirectorySegments.Any(segment => normalised.Contains($"/{segment}/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Walks up from the test binary until the directory holding the solution is found, so the
    /// scan does not depend on the depth of the build output path.
    /// </summary>
    static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("DiceToSeed.slnx").Any())
            directory = directory.Parent;

        return directory ?? throw new InvalidOperationException(
            $"Could not find DiceToSeed.slnx above {AppContext.BaseDirectory}. The entropy scan cannot be trusted until it can locate the source.");
    }

    static string Relative(string file) =>
        Path.GetRelativePath(RepositoryRoot().FullName, file).Replace('\\', '/');
}
