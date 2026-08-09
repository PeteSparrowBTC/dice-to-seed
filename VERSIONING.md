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

The number lives in three files and they must move together, because artifacts that disagree
about what they are cannot be verified:

- `Directory.Build.props` (`Version`, `AssemblyVersion`, `FileVersion`)
- `src-tauri/Cargo.toml` (`version`)
- `src-tauri/tauri.conf.json` (`version`)

Then, in order:

1. Commit the bump and the `CHANGELOG.md` entry on a branch.
2. Open a pull request; a human merges it. `main` never moves any other way.
3. Only once the release commit is on `main`, push the tag: `git push origin v1.2.3`.

That order is not bureaucracy. Pushing the tag publishes a GitHub Release, and the release is
what people run against real money. It must be built from a commit that has been through
review, not from a branch.

## What a release contains

- `dice-to-seed-x86_64.AppImage`: the Tails route, one file
- `dice-to-seed-wwwroot.zip`: the same application in files a person can read
- `SHA256SUMS`

The static site ships alongside the AppImage deliberately. An AppImage is an opaque blob and
the checksum is all a downloader can check it against; the zip is the same program in a form
that can be inspected.

The tests gate the release. The published vendor vectors are the reason to trust any of this,
so a release that has not run them is not a release.

---

*Collaboration by Claude*
