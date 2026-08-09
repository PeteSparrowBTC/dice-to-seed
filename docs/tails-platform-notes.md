# What Tails provides, and how to check it without booting Tails

Checked against **Tails 7.10.1** (Debian 13 "trixie" base, `base-files 13.8+deb13u6`) on
2026-08-09. Do not re-test this by hand. Re-check it the way it was checked the first time,
which takes about ten seconds.

## The method: read the official package manifest

Tails publishes the exact list of packages in every release, so what a Tails session contains
is a fact you can look up rather than something to discover by booting a USB stick and
squinting at error messages.

```bash
# find the current release
curl -s https://tails.net/install/download/ | grep -oE "tails-amd64-[0-9.]+" | sort -u

# fetch its package manifest (about 1900 packages, name and version, tab separated)
curl -s -o tails.packages https://tails.net/torrents/files/tails-amd64-7.10.1.packages

# ask whatever you need to know
grep -iE "webkit|fuse|gtk-3" tails.packages
```

Replace the version as releases move. The manifest is the same file the torrents are built
from, so it is authoritative for what ships.

## Verified present in 7.10.1

Everything `Photino.Native.so` links against, which is the whole runtime requirement of the
desktop AppImage:

| shared object needed | provided by | version |
| --- | --- | --- |
| `libwebkit2gtk-4.1.so.0` | `libwebkit2gtk-4.1-0` | 2.52.5-1~deb13u1 |
| `libjavascriptcoregtk-4.1.so.0` | `libjavascriptcoregtk-4.1-0` | 2.52.5-1~deb13u1 |
| `libgtk-3.so.0`, `libgdk-3.so.0` | `libgtk-3-0t64` | 3.24.49-3 |
| `libglib-2.0`, `libgio-2.0`, `libgobject-2.0` | `libglib2.0-0t64` | 2.84.4-3~deb13u3 |
| `libnotify.so.4` | `libnotify4` | 0.8.6-1 |
| `libgcc_s.so.1` | `libgcc-s1` | 14.2.0-19 |
| `libstdc++.so.6` | `libstdc++6` | 14.2.0-19 |
| `libc.so.6` | base | |

Also present and worth knowing:

- **`libfuse2t64` 2.9.9-9.** AppImages self-mount through FUSE 2. Debian 13 moved to FUSE 3
  and many systems no longer carry FUSE 2, which is the usual reason an AppImage does nothing
  on a modern distribution. Tails still has it, so `--appimage-extract-and-run` is not needed.
- **`fuse3` and `libfuse3-4` 3.17.2-3**, alongside it.
- **GTK 4** (`libgtk-4-1` 4.18.6) as well as GTK 3, though Photino uses GTK 3.

**WebKitGTK is 4.1 only.** Debian 13 dropped the 4.0 series. Anything linked against
`libwebkit2gtk-4.0.so.37` will not run on Tails 7. Photino 4.x links 4.1, so it is fine; this
was confirmed twice, once from `slip39-backup`'s AppImage (known to run on Tails) and once
from the CI dependency report on this repository's own build.

## Things that are not about missing libraries

When the AppImage appears to do nothing on Tails, the cause has so far never been a missing
dependency. In order of likelihood:

1. **GNOME Files does not launch binaries on double-click.** Modern GNOME removed that
   behaviour and does it silently. Run it from a terminal instead.
2. **The executable bit is gone**, because the file crossed a FAT or exFAT stick, or came from
   Windows. `chmod +x`.
3. **The USB may be mounted `noexec`.** Copy the AppImage to `~` first; that removes causes 2
   and 3 together.
4. **A pre-flight check in `AppRun` that is itself broken.** This actually happened: the check
   ran `ldconfig -p`, but `ldconfig` lives in `/usr/sbin` and is not on a normal user's PATH on
   Debian, so the command did not exist, its error went to the `/dev/null` the check itself
   supplied, and the script refused to start on a system where the library was installed all
   along. `AppRun` now warns rather than blocking, and CI prints the real `NEEDED` entries on
   every build. A guard that can produce a false negative is worse than no guard.

## Browser notes

- **LibreWolf, not Tor Browser**, for the local server route. Tor Browser on Tails sends
  `127.0.0.1` through the Tor proxy and refuses the connection. It can be fixed under
  `about:preferences`, Network Settings, "No Proxy for", but the setting does not reliably
  survive. Carry the LibreWolf AppImage on the stick, downloaded before booting Tails.
- `file://` needs no proxy exception, which is why a single-file HTML build would sidestep the
  browser question entirely if the desktop route is ever abandoned.

---

*Collaboration by Claude*
