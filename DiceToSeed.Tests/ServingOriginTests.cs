using DiceToSeed.Core;

namespace DiceToSeed.Tests;

/// <summary>
/// The banner decision, which the Pages workflow describes as "the only thing standing between a
/// curious visitor and a real roll log typed into a web page". That comment also said the behaviour
/// was tested, and until this file existed it was not.
///
/// The cases below are split into three groups on purpose. The permissive failures are the
/// dangerous ones: a host that is wrongly treated as local shows the reassuring banner to somebody
/// on a web page, and a roll log is the seed in plaintext. The restrictive failures are merely
/// annoying, warning someone who is running it correctly.
/// </summary>
public class ServingOriginTests
{
    /// <summary>
    /// Local, so the ordinary banner. The last two are the AppImage: its WebView serves through an
    /// in-process handler rather than over a network, and Tauri's scheme and host differ by
    /// platform, so both forms have to pass or the intended way to run this app warns against
    /// itself.
    /// </summary>
    [Theory]
    [InlineData("http://127.0.0.1:9876/")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://localhost:5000/")]
    [InlineData("http://LOCALHOST/")]
    [InlineData("https://localhost/")]
    [InlineData("http://[::1]:5000/")]
    [InlineData("http://127.0.0.2/")]
    [InlineData("http://127.15.16.17/")]
    [InlineData("tauri://localhost/")]
    [InlineData("http://tauri.localhost/")]
    [InlineData("file:///home/amnesia/index.html")]
    public void Local_origins_do_not_get_the_loud_warning(string uri) =>
        Assert.True(ServingOrigin.IsLocal(new Uri(uri)), $"{uri} should be treated as local.");

    /// <summary>
    /// Served by somebody else, so the loud warning. The first two are the hosted demo. The rest
    /// are the ones a substring or prefix check waves through, which is the whole reason this is a
    /// function with tests rather than an expression inline in the page.
    /// </summary>
    [Theory]
    [InlineData("https://petesparrowbtc.github.io/dice-to-seed/")]
    [InlineData("http://example.com/")]
    [InlineData("http://127.0.0.1.example.com/")]
    [InlineData("http://localhost.evil.com/")]
    [InlineData("http://notlocalhost/")]
    [InlineData("http://mylocalhost/")]
    [InlineData("http://localhosts/")]
    [InlineData("http://8.8.8.8/")]
    [InlineData("http://127.0.0.1.evil.co.uk/")]
    public void Remote_origins_get_the_loud_warning(string uri) =>
        Assert.False(ServingOrigin.IsLocal(new Uri(uri)), $"{uri} must NOT be treated as local.");

    /// <summary>
    /// The described origin carries the scheme, because "localhost" alone cannot tell an ordinary
    /// local server from the AppImage's in-process handler, and that distinction was unanswerable
    /// from the page when it mattered.
    /// </summary>
    [Theory]
    [InlineData("http://127.0.0.1:9876/", "http://127.0.0.1:9876")]
    [InlineData("http://localhost/", "http://localhost")]
    [InlineData("https://petesparrowbtc.github.io/dice-to-seed/", "https://petesparrowbtc.github.io")]
    [InlineData("tauri://localhost/", "tauri://localhost")]
    public void The_origin_is_described_with_its_scheme(string uri, string expected) =>
        Assert.Equal(expected, ServingOrigin.Describe(new Uri(uri)));

    /// <summary>
    /// The mistake this replaced. Keeping it as a test states which shortcuts are wrong, so the
    /// next person to simplify this has to fail something rather than reason about it.
    /// </summary>
    [Fact]
    public void A_substring_check_would_have_been_wrong()
    {
        const string attacker = "http://127.0.0.1.example.com/";

        // What a careless implementation does, and why it is not safe.
        Assert.Contains("127.0.0.1", new Uri(attacker).Host);
        Assert.False(ServingOrigin.IsLocal(new Uri(attacker)));
    }
}
