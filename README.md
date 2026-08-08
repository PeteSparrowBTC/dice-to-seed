# dice-to-seed

An offline Blazor WebAssembly app that turns a log of six-sided dice rolls into a BIP-39 seed
phrase, using the convention Coldcard, SeedSigner and Krux all use for d6.

**Physical dice only, and Tails first.** The app has no random number generator and never will:
no "roll for me" button, no simulated die, nothing random anywhere in the derivation. It
converts rolls you made with real dice, and a test fails the build if a random source ever
appears in the source. The intended way to run it is from a USB stick on an offline Tails
session, served on `127.0.0.1` by a local web server, which is required because Blazor
WebAssembly does not load over `file://`. See
[TAILS_INSTRUCTIONS.md](TAILS_INSTRUCTIONS.md).

## What this is for

This app is a **second implementation**, not a recommended primary generator. Its purpose is
to be checked against: you enter the same roll log into a hardware wallet, into this app, and
into a third tool, and confirm all three produce the same words before any money is involved.

That matters because you cannot audit entropy after the fact. A seed from a broken random
number generator looks exactly like a seed from a good one. In July 2026 Coldcard firmware was
found to have skipped its hardware generator entirely for five years, and Coinkite's guidance
was that seeds with at least 50 fair, independent, private dice rolls were not at risk. Dice
supply provenance; a second implementation is what confirms the device honoured it.

## The conversion

```
entropy  = SHA-256(the roll digits, joined by nothing)   truncated to 16 bytes for 12 words,
                                                          32 bytes for 24
checksum = the top 4 or 8 bits of SHA-256(that entropy)
words    = (entropy || checksum) split into 11-bit indexes into the 2048-word list
```

The hash is **truncated, not re-hashed**, and the checksum comes from the truncation. Both
mistakes still produce twelve plausible words, which is why the test suite pins them.

Minimums are the vendors': **50 rolls for 12 words, 99 for 24.** A short log does not become
stronger by producing more words.

## What it deliberately does not do

No BIP-32, no addresses, no master fingerprints, no SLIP-39 splitting, no saving or exporting,
no clipboard button, and no built-in entropy source. It converts entropy you brought and shows
you every intermediate value. Splitting a finished seed is the sibling
[slip39-backup](https://github.com/PeteSparrowBTC/slip39-backup) app; the vendor-neutral
reference on generating one is
[seed-generation](https://github.com/PeteSparrowBTC/seed-generation).

There is also no separator or dialect control. All three d6 vendors hash the bare digit
string; Krux's dash convention belongs to d20, where a face value can be two digits. A dash
setting in a d6-only tool would produce a seed no vendor reproduces.

## Verification built into the suite

- the complete published BIP-39 English vector set, as upstream's file byte for byte, with its
  SHA-256 asserted so it cannot be edited into agreement
- Coldcard's published dice example, confirmed by running Coldcard's own `rolls12.py`
- SeedSigner's published 50-roll and 99-roll examples
- the wordlist checked at runtime against its published SHA-256; the app refuses to derive
  anything if it does not match
- a guard that fails the build if `RandomNumberGenerator`, `System.Random`,
  `crypto.getRandomValues` or similar ever appears in first-party source

## Running it locally

```bash
dotnet test                                   # the vectors
dotnet run --project DiceToSeed.Web           # a dev server on loopback
```

For real use, publish it and follow [TAILS_INSTRUCTIONS.md](TAILS_INSTRUCTIONS.md):

```bash
cd DiceToSeed.Web
dotnet publish -c Release -o publish          # publish/wwwroot goes on the USB stick
```

## Working here

Read [CLAUDE.md](CLAUDE.md) first. In short: `main` moves only through a pull request, the app
is never a source of entropy, the conversion takes no cryptographic dependency beyond SHA-256,
the vectors come from the vendors' own published output, and every algorithmic change re-runs
them.

After cloning:

```bash
git config core.hooksPath .githooks
```

---

*Collaboration by Claude*
