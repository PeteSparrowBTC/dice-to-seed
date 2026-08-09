# Changelog

Semantic versioning, with MAJOR reserved for a change that would produce different words for
the same rolls. See [VERSIONING.md](VERSIONING.md).

## 1.0.0

First release.

### The app

- Converts a log of six-sided dice rolls into a BIP-39 seed phrase, 12 or 24 words, using the
  convention Coldcard, SeedSigner and Krux all share for d6: the bare digit string, hashed with
  SHA-256, truncated rather than re-hashed, checksum taken from the truncation.
- Rolls are recorded with six dice-face buttons rather than typed, so nothing but a die face
  can enter the log. Keys 1 to 6 and Backspace do the same.
- Shows every intermediate value, and renders the exact preimage it hashes so it can be
  compared character for character against another tool.
- Vendor minimums, unmodified: 50 rolls for 12 words, 99 for 24.

### Verification

- The complete published BIP-39 English vector set, as upstream's file byte for byte, with its
  SHA-256 asserted so it cannot be edited into agreement.
- Coldcard's published dice example, confirmed by downloading and running Coldcard's own
  `rolls12.py`.
- SeedSigner's published 50-roll and 99-roll examples.
- The wordlist verified at startup against its published SHA-256; the app refuses to derive
  anything if it does not match.

### What it will never do

- No random number generator of any kind, enforced by a test that scans first-party source and
  fails the build. No "roll for me", no simulated die, no nonce, no id.
- No storage, no network call, no telemetry, no clipboard writes, no BIP-32, no addresses.

### Distribution

- An 11 MB AppImage for Tails, built with Tauri and packaged without bundling WebKitGTK, which
  Tails already ships. Verified against the Tails 7.10.1 package manifest rather than assumed.
- The static site, published alongside it, as the artifact to read.
- A demonstration build on GitHub Pages carrying a banner that cannot be dismissed.

### Notes for the curious

Two findings from building this are recorded in the repository because they change what a
careful person does:

- There is **no d6 dialect split**. Krux joins d6 rolls with nothing, exactly as Coldcard and
  SeedSigner do, and reserves the dash for d20 where a face value can be two digits. An earlier
  plan for this app was wrong about that, and a separator control was removed rather than
  shipped.
- **A roll log of all 1s cannot detect the most dangerous misconfiguration** in Ian Coleman's
  page, because its "Dice" entropy type differs from "Base 10" only where a 6 appears. The
  easiest log to type is the one that proves least.
