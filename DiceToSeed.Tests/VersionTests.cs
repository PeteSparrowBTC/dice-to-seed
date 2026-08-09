using System.Reflection;
using System.Text.RegularExpressions;

namespace DiceToSeed.Tests;

/// <summary>
/// The version is written by hand in exactly one place, the VERSION file at the repository root,
/// and everything else derives from it or is checked against it here.
///
/// Why this is worth a test. A release publishes an AppImage, a zip of the same application, and
/// a git tag, and the only thing a downloader can do with an opaque AppImage is check what it
/// claims to be. Artifacts that disagree about their own version cannot be verified against the
/// release they came from, and the way that used to happen was five fields across three files,
/// bumped together from memory at the end of a long session.
///
/// What derives, and what is merely checked:
///
///   Directory.Build.props   reads VERSION with an MSBuild property function, so Version,
///                           AssemblyVersion and FileVersion cannot drift by construction
///   tauri.conf.json         has no version field. Tauri falls back to Cargo.toml: "If removed
///                           the version number from Cargo.toml is used", tauri-utils config.rs
///   src-tauri/Cargo.toml    still holds a literal, because Cargo has no mechanism for reading a
///                           value out of another file and the manifest is parsed before build.rs
///                           runs. That one copy is what the tests below exist for
///
/// Same approach as the entropy scan and the wordlist hash: the rule is enforced by something
/// that fails, rather than written down and relied on.
/// </summary>
public class VersionTests
{
    [Fact]
    public void The_version_file_holds_a_plain_three_part_version()
    {
        var version = DeclaredVersion();

        Assert.Matches(@"^\d+\.\d+\.\d+$", version);
        // The tag is v-prefixed and the file is not; a "v" here would produce "vv1.1.0" or a tag
        // that does not match the artifacts.
        Assert.DoesNotContain("v", version, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The one copy Cargo forces. If this fails, the AppImage would be published claiming a
    /// different version from the assemblies inside it.
    /// </summary>
    [Fact]
    public void The_rust_manifest_agrees_with_the_version_file()
    {
        var manifest = File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "src-tauri", "Cargo.toml"));

        // The [package] version, not a dependency's: anchored to the start of a line and taken
        // as the first match, which is inside [package] because it is the first table.
        var declared = Regex.Match(manifest, @"^version\s*=\s*""(?<version>[^""]+)""", RegexOptions.Multiline);

        Assert.True(declared.Success, "No package version found in src-tauri/Cargo.toml.");
        Assert.Equal(DeclaredVersion(), declared.Groups["version"].Value);
    }

    /// <summary>
    /// Proves the MSBuild side actually read the file, rather than the build having silently
    /// fallen back to a default while VERSION sat there being ignored. A test that only compared
    /// two text files would pass in exactly that case.
    /// </summary>
    [Fact]
    public void The_compiled_assembly_carries_the_version_from_the_file()
    {
        var assemblyVersion = typeof(DiceToSeed.Core.DiceSeed).Assembly.GetName().Version;

        Assert.NotNull(assemblyVersion);
        Assert.Equal($"{DeclaredVersion()}.0", assemblyVersion.ToString());
    }

    /// <summary>
    /// tauri.conf.json must NOT carry a version. With one present it wins over Cargo.toml, which
    /// would quietly reintroduce the third copy this arrangement exists to remove, and nothing
    /// else here would notice.
    /// </summary>
    [Fact]
    public void The_tauri_config_does_not_declare_its_own_version()
    {
        var config = File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "src-tauri", "tauri.conf.json"));

        Assert.DoesNotContain("\"version\"", config, StringComparison.Ordinal);
    }

    static string DeclaredVersion() =>
        File.ReadAllText(Path.Combine(RepositoryRoot().FullName, "VERSION")).Trim();

    static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("DiceToSeed.slnx").Any())
            directory = directory.Parent;

        return directory ?? throw new InvalidOperationException(
            $"Could not find DiceToSeed.slnx above {AppContext.BaseDirectory}. The version check cannot be trusted until it can locate the repository.");
    }
}
