namespace DiceToSeed.Core;

/// <summary>
/// Whether this copy of the app is running locally or is being served to you by somebody else.
///
/// This decides which banner the page shows, and that banner is the only thing standing between a
/// curious visitor to the hosted demo and a real roll log typed into a web page. A roll log is the
/// seed in plaintext before any hashing, so getting this wrong in the permissive direction is the
/// worst single mistake the UI can make.
///
/// It lives in Core, tested, because it used to be one expression inline in the page and the Pages
/// workflow claimed "that behaviour is tested" when nothing tested it. A safety property asserted
/// in a comment is not a safety property.
///
/// What counts as local, and why each case is here rather than only the obvious two:
///
///   a non-web scheme          tauri:, file: and the like are not served over a network at all;
///                             they are an in-process handler reading bytes off the disk. The
///                             AppImage's WebView loads through one of these, so it must not be
///                             warned at
///   the exact host localhost  the ordinary case, matched case-insensitively because host names are
///   any loopback IP literal   127.0.0.1, the rest of 127.0.0.0/8, and ::1 in either bracketed or
///                             bare form. Serving on 127.0.0.2 is no less local than 127.0.0.1
///   anything under .localhost RFC 6761 reserves that name for loopback and browsers resolve it
///                             without asking DNS. Tauri serves from tauri.localhost on some
///                             platforms, and that is a local origin by the same rule
///
/// What must NOT count, and these are the reason this is a function rather than a substring test:
/// 127.0.0.1.example.com and localhost.evil.com are ordinary internet hosts that a naive
/// "contains" or "starts with" check would wave through, handing the attacker exactly the silence
/// they want.
/// </summary>
public static class ServingOrigin
{
    /// <summary>
    /// True when the app is running locally, so the page may show the ordinary banner. False means
    /// somebody else is serving it and the loud warning belongs on screen.
    /// </summary>
    public static bool IsLocal(Uri uri)
    {
        // Not http or https: nothing crossed a network to get here. The AppImage lands in this
        // branch, whichever scheme its WebView happens to use.
        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return true;

        // DnsSafeHost strips the brackets IPv6 literals carry in Host, so ::1 parses either way.
        var host = uri.DnsSafeHost;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        // Whole-label suffix, never a substring: "evil.localhost" is loopback by RFC 6761,
        // "localhost.evil.com" is not, and only one of them ends with ".localhost".
        if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return System.Net.IPAddress.TryParse(host, out var address)
               && System.Net.IPAddress.IsLoopback(address);
    }

    /// <summary>
    /// What to put on screen: scheme and host together, with the port when there is one.
    ///
    /// The scheme is included deliberately. Without it a reader cannot tell an ordinary local
    /// server from the AppImage's in-process handler, and neither could I when I needed to know
    /// which one the AppImage reports.
    /// </summary>
    public static string Describe(Uri uri) =>
        uri.IsDefaultPort ? $"{uri.Scheme}://{uri.Host}" : $"{uri.Scheme}://{uri.Host}:{uri.Port}";
}
