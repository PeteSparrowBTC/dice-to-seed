# dice-to-seed

An offline Blazor WebAssembly app that turns a log of six-sided dice rolls into a BIP-39 seed
phrase, using the same convention Coldcard and SeedSigner use.

**Status: planned, not yet built.** Read
[the implementation plan](docs/superpowers/plans/2026-08-08-dice-to-seed-app.md) first.

## What this is for

This app is a **second implementation**, not a recommended primary generator. Its purpose is
to be checked against: you enter the same roll log into a hardware wallet, into this app, and
into a third tool, and confirm all three produce the same words before any money is involved.

That matters because you cannot audit entropy after the fact. A seed from a broken random
number generator looks exactly like a seed from a good one. In July 2026 Coldcard firmware was
found to have skipped its hardware generator entirely for five years, and Coinkite's guidance
was that seeds with at least 50 fair, independent, private dice rolls were not at risk. Dice
supply provenance; a second implementation is what confirms the device honoured it.

## What it deliberately does not do

No BIP-32, no addresses, no master fingerprints, no SLIP-39 splitting, no saving or exporting,
and no built-in entropy source. It converts entropy you brought and shows you every
intermediate value. Splitting a finished seed is the sibling
[slip39-backup](https://github.com/PeteSparrowBTC/slip39-backup) app; the vendor-neutral
reference on generating one is
[seed-generation](https://github.com/PeteSparrowBTC/seed-generation).

## Working here

Read [CLAUDE.md](CLAUDE.md) first. In short: `main` moves only through a pull request, the
conversion takes no cryptographic dependency beyond SHA-256, and every algorithmic change
re-runs the published test vectors.

After cloning:

```bash
git config core.hooksPath .githooks
```

---

*Collaboration by Claude*
