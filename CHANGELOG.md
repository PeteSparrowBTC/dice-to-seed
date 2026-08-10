# Changelog

Semantic versioning, with MAJOR reserved for a change that would produce different words for
the same rolls. See [VERSIONING.md](VERSIONING.md).

## 1.3.0

The same words from the same rolls as 1.2.0. This release is what came out of running the AppImage
on Tails and reading the page as a user rather than as its author.

### The page is ordered for someone using it

- **"Before you start" is above the dice pad**, where advice about what to do first belongs. It was
  at the bottom, past the point where anyone had already started rolling. It is collapsible and open
  on load, because expanded it pushes the dice below the fold in the 1000x900 window the AppImage
  opens at: the pad sits at y=852 open and y=657 collapsed. Read it, close it, roll.
- The entropy table moved behind its own disclosure. Fifty lines of arithmetic is not what "before
  you start" means.

### The word "preimage" is gone from the interface

- The result said "Preimage (the rolls, hashed as-is)", which claimed the value shown was already a
  digest and used jargon to do it. The labels now read **What will be hashed**, **What was hashed:
  your rolls, and nothing else**, and **SHA-256 of that string**. For d6 the value is the roll log
  character for character, and the label says so instead of naming the concept.

### The backup key can be copied, and says where it goes

- **The key was renderable only as sixteen numbered groups**, and selecting that block dragged the
  group numbers into the clipboard, so a paste produced `1 6cb0 2 9af8 3 5505` rather than the key.
  The unbroken 64-character line is now the primary rendering, with the grouped view kept below it
  and labelled for pen and paper.
- The group numbers became CSS generated content on a data attribute. `user-select: none` was tried
  first and is not enough: it prevents a mouse selecting the number while leaving the text in the
  DOM, so a copy still reaches it. Generated content cannot be copied by any route.
- The key and the check code are now boxed together under **"This is what goes into
  slip39-backup"**, followed by "Those two values, and nothing else on this page". They used to be
  three step labels apart with commentary in between, so which values to transfer was a fair
  question.

### The banner decision is tested

- The Pages workflow described the red warning on a hosted build as "the only thing standing between
  a curious visitor and a real roll log typed into a web page", and said the behaviour was tested.
  Nothing tested it: it was an inline expression comparing the host against three strings. It now
  lives in `DiceToSeed.Core/ServingOrigin.cs` with 25 cases, including the hosts a shortcut waves
  through, such as `127.0.0.1.example.com` and `localhost.evil.com`.
- The check also got wider where it was too narrow, and one case was a latent defect: a non-web
  scheme is local, because `tauri:` and `file:` are an in-process handler rather than a network; the
  whole loopback range counts, not only `127.0.0.1`; and anything under `.localhost` counts, per RFC
  6761. Tauri serves from `tauri.localhost` on Windows, so a Windows desktop build would have warned
  against itself on every launch.

### Packaging

- **The AppImage filename carries the version**: `dice-to-seed-1.3.0-x86_64.AppImage`. The AppImage
  is the file that survives on somebody's USB stick long after `SHA256SUMS` has been deleted, and a
  bare name tells its owner nothing about which release they hold. Anything scripted against
  `dice-to-seed-x86_64.AppImage` needs updating; the release notes and docs glob on the pattern.

### Honesty about the clipboard

- Rule 6 listed "no clipboard writes of seed material" beside "no network call", which reads as a
  safeguard. It is not one: any text on the page can be selected and copied. What is true, and what
  the rule and the footer now say, is that the app never writes to the clipboard itself, so nothing
  lands there unless you put it there. Copy buttons were considered and not added.

## 1.2.0

The same words from the same rolls as 1.1.0. This release is about the page telling the truth and
being usable by someone who is not comfortable in a terminal.

### The vendor minimums are not sufficiency proofs

- A fair d6 carries log2(6) = 2.585 bits, so **99 rolls carry 255.9 bits against a target of 256**.
  The 24-word minimum does not reach its target even with a perfect die, and the page said nothing
  about it. It does now, with a table.
- Recommends **60 rolls for 12 words and 111 for 24** when making a new seed, chosen so the
  conservative min-entropy floor clears the target rather than the average just about reaching it.
  The counter targets those numbers, and a result derived below them carries a warning beside the
  words.
- The minimum stays at the vendor numbers of 50 and 99. Coinkite's advice after the 2026 firmware
  defect was that seeds of at least 50 rolls were unaffected, so the people with the strongest
  reason to use this app hold 50-roll seeds and must be able to reproduce them.

### The runbook was unusable in two ways

- **Commands referred to `$ROLLS` and never set it.** An unset variable expands to nothing, so
  anyone following the runbook hashed the empty string and got `e3b0c442...b855` every time: a
  plausible hash matching nothing, whose obvious reading is that the app is wrong. Every command
  now carries your actual rolls, so a line goes straight into a terminal.
- **The hardest cross-check came first.** Now ordered easiest first: Coleman's page needing no
  terminal, then Coldcard's `rolls12.py` as one command, then Trezor's reference library with
  numbered preparation steps.
- The hash commands are labelled as checking step 2 rather than the words, because `sha256sum`
  knows nothing about BIP-39.
- Coleman's page has an address now, and the Dice-versus-Base-10 trap is something you can watch
  happen on his Filtered Entropy line rather than take on trust.

### Backup key mode

- The output is `k`, and `k` is the dice and nothing else. Mixing in a generated value by XOR was
  considered and rejected: it would hedge a biased die at the cost of making `k` impossible for
  anyone to recompute, so nobody could confirm the tool used the dice they rolled.
- States its limit: `k` wraps the backup, while the file key inside the age format is generated by
  the consuming tool and is what the payload is encrypted under.

### The page says which build it is

- Footer shows the version and the commit, so a build can be checked against a release tag.

### Tails, not a suggestion

- The banner says to use Tails and nowhere else, with the reason. The claim that there is no copy
  button because the app cannot tell where it is running is gone: it was never a protection, since
  any text on the page can be selected and copied.

### The release is one file

- `dice-to-seed-wwwroot.zip` and the local-server browser route are dropped. The AppImage runs on
  Tails from the file manager, so the second route was a second set of instructions with its own
  port to check and its own Tor Browser proxy caveat. The zip was also described as the artifact "a
  person can read", which oversold it: 142 of its 155 files were the compiled runtime.
- **The demo now deploys on a version tag**, the same event that publishes the release, so the two
  are always the same version.

### Fixes

- The favicon was still the Blazor template's purple logo. It is now the die that the AppImage
  uses, as an SVG.
- The external-reference check in all three workflows failed on `favicon.svg`, because an SVG must
  declare the `http://www.w3.org/2000/svg` namespace and the check could not tell an XML identifier
  from an address. Namespace values are now stripped before the check, and a namespace sharing a
  line with a genuine external URL is still caught.

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
