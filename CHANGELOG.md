# Changelog

Semantic versioning, with MAJOR reserved for a change that would produce different words for
the same rolls. See [VERSIONING.md](VERSIONING.md).

## 1.1.0

A second mode, for the key that encrypts your backup. The seed conversion is untouched: the same
rolls produce the same words as 1.0.0.

### The backup key mode

- Rolls for `k`, the 32-byte key [slip39-backup](https://github.com/PeteSparrowBTC/slip39-backup)
  encrypts with and splits into shares. That key otherwise comes from a generator nobody can
  check, which is the thing this app avoids everywhere else.
- `k = SHA-256(the bare digit string)`, all 32 bytes. No new convention: it is the value the seed
  mode already shows at step 2, and `printf '%s' "$ROLLS" | sha256sum` reproduces it.
- **Renders hex and never words.** That is what makes a mode selector safe to offer here. A mode
  whose wrong position still produces plausible output is Ian Coleman's "Dice versus Base 10"
  trap: a different wallet, no warning. Hex and words differ in kind, so a mis-set mode shows.
- **Switching mode clears the roll log**, with a confirmation that gives the reason. One log must
  never yield both: on 24 words the BIP-39 entropy is that hash byte for byte, so a reused log
  makes `k` identical to the wallet it protects and the shares stop protecting anything.
- Shows a four character check code, because `k` is transcribed by hand and, unlike words and
  shares, carries no checksum of its own: any string is a valid passphrase, so a mistyped key
  encrypts cleanly and is discovered at recovery. The code is the first four characters of the
  hex SHA-256 of the printed hex key, computed over the string on screen so a shell reproduces it
  without decoding.
- Optional, and the documentation says what it does not buy: AgeSharp fills the age file key from
  its own generator and encrypts the payload under that, and `k` only wraps it. Dice give `k` a
  provenance you can account for, which is a smaller claim than removing every generator. Rolling
  for the seed remains mandatory, because entropy quality is the one property no later step can
  check.

### Fixes

- `TAILS_INSTRUCTIONS.md` offered Ian Coleman's page as a cross-check without saying to set the
  entropy type to Base 10 rather than Dice. The Dice type rewrites every 6 to a 0 before hashing,
  so a reader following the old text had a coin flip between the right answer and a confident
  wrong one. It now also says to test the setting with a log containing a 6, since the two types
  agree on every log without one.

### Under it

- The roll minimum is now stated against an entropy target rather than a word count, which is
  what always determined it. A test asserts both paths give the same number.
- Ten new tests, including one that pins the reuse hazard rather than a feature: it asserts that
  the same log makes `k` identical to the 24-word entropy, so if the derivation ever moves, the
  warnings built on it get revisited.

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
