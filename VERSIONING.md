# Versioning

[Semantic versioning](https://semver.org), with one addition that matters more than the rest.

## What the public interface is here

For most software the interface is an API. For this app it is **the words**. A user's entire
relationship with it is: these rolls produce these words, and my hardware wallet agrees.

So:

> **MAJOR is reserved for a change that produces different words for the same rolls.**

That is the only breaking change this app can make, and it would be a catastrophic one: someone
who verified a seed against version 1 and re-verifies against version 2 would be told their
wallet is wrong when nothing had happened to it. There is no known reason ever to do this. If
the day comes, it is a MAJOR bump, a new name in the release title, and an explanation at the
top of the release notes.

Everything else is smaller than it sounds:

| | means | examples |
| --- | --- | --- |
| **MAJOR** | the derivation changed | different words for identical rolls. Must never happen without extraordinary justification |
| **MINOR** | new capability, same words | 24-word support, a new packaging target, a new cross-check in the runbook |
| **PATCH** | fixes and text, same words | a UI defect, a wrong instruction, dependency bumps, documentation |

A useful way to read that table: **almost everything is MINOR or PATCH.** If a change makes you
reach for MAJOR, stop and check whether you have accidentally altered the conversion.

## Bumping a version

The number is written by hand in **one place**, the `VERSION` file at the repository root, and
everything else derives from it or is checked against it. Edit that file.

```
1.2.3
```

Where it goes, and why the arrangement is shaped this way:

| | how it gets the version |
| --- | --- |
| `Directory.Build.props` | reads `VERSION` directly, so `Version`, `AssemblyVersion` and `FileVersion` cannot drift by construction |
| `src-tauri/tauri.conf.json` | has no `version` field at all. Tauri falls back to `Cargo.toml`, so the AppImage follows automatically |
| `src-tauri/Cargo.toml` | the one remaining literal, and it must be edited too |

Cargo has no mechanism for reading a value out of another file: the manifest is parsed before
anything in the crate runs, so `build.rs` cannot influence it. That copy is therefore checked
rather than trusted. `VersionTests` fails if it disagrees with `VERSION`, if `VERSION` is not a
plain three-part number, if `tauri.conf.json` reacquires a `version` field, or if the compiled
assembly does not carry what the file says. The last of those is the one that matters most: it
proves the build actually read the file, which a comparison between two text files would not.

Artifacts that disagree about what they are cannot be verified, and an AppImage is opaque enough
that its claimed version is most of what a downloader has to go on.

Then, in order:

1. Commit the bump and the `CHANGELOG.md` entry on a branch.
2. Open a pull request; a human merges it. `main` never moves any other way.
3. Only once the release commit is on `main`, push the tag: `git push origin v1.2.3`.

That order is not bureaucracy. Pushing the tag publishes a GitHub Release, and the release is
what people run against real money. It must be built from a commit that has been through
review, not from a branch.

## What a release contains

- `dice-to-seed-<version>-x86_64.AppImage`: the whole app, one file. The version is in the name
  because the AppImage is what survives on somebody's USB stick, long after `SHA256SUMS` has been
  deleted, and a bare filename tells its owner nothing about which release they are holding
- `SHA256SUMS`

An AppImage is an opaque blob and the checksum is all a downloader can check it against. A
`wwwroot` zip used to ship alongside it, described as the same program in a form that can be
inspected. That claim did not hold up: the markup and the stylesheet are readable, and the
derivation is compiled to WebAssembly in both artifacts. Reading this app means reading the
source, and running it in a browser is what the Pages demo is for.

The tests gate the release. The published vendor vectors are the reason to trust any of this,
so a release that has not run them is not a release.

---

*Collaboration by Claude*
