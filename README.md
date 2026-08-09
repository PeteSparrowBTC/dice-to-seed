# dice to seed

Turns dice rolls into a BIP-39 seed phrase, offline.

Demo: **https://petesparrowbtc.github.io/dice-to-seed/**

Not for a real seed. The rolls you type are the seed, and from a web page you cannot confirm
the files you were served are the ones in this repository.

## What it is for

Your hardware wallet made a seed from your dice. This app converts the same rolls separately,
so you can check that the words match.

A seed from a broken random number generator looks like any other. In July 2026 Coldcard
firmware was found to have skipped its hardware generator for five years, and Coinkite said
seeds made with at least 50 dice rolls were unaffected. Dice give you randomness you can
account for; comparing the words is how you confirm the device used it.

## What you need

- Dice and paper
- Tails, with the network off
- A USB stick

## Use

Before booting Tails, download the AppImage from the
[releases page](https://github.com/PeteSparrowBTC/dice-to-seed/releases) and check it:

```bash
sha256sum -c SHA256SUMS
```

Copy it to the stick. On Tails:

```bash
cp /media/amnesia/*/dice-to-seed-x86_64.AppImage ~/
cd ~ && chmod +x dice-to-seed-x86_64.AppImage
./dice-to-seed-x86_64.AppImage
```

Run it from a terminal. Double-clicking does nothing, because the Tails file manager will not
launch programs.

Roll one die at a time and press the matching face. Fifty rolls for twelve words, ninety-nine
for twenty-four. Keys 1 to 6 work, and Backspace undoes the last roll. Watch the counter: a log
one roll short still produces a valid-looking seed phrase. Write the words on paper.

Full instructions, including how to check the result against other tools:
[TAILS_INSTRUCTIONS.md](TAILS_INSTRUCTIONS.md).

## Two mistakes to avoid

Do not re-roll a log because it looks wrong. Fifty 1s is as likely as any other fifty rolls.
Discarding logs narrows the set your seed is drawn from.

If you throw several dice at once, use dice you can tell apart and read them in the same order
every time. Four identical dice lose about a third of their entropy, and the roll counter
cannot detect it.

## Limits

No random number generator. Every value comes from your dice, and a test fails the build if a
source of randomness appears in the code.

No saving, exporting, copying or network access. No addresses, no wallet features.

## Checks

The test suite runs these on every change:

- SeedSigner's published examples
- Coldcard's published example, confirmed against their own script
- the official BIP-39 test vectors, the complete set
- the wordlist, against its published hash, at startup

## Related

[slip39-backup](https://github.com/PeteSparrowBTC/slip39-backup) splits a seed you already
have. [seed-generation](https://github.com/PeteSparrowBTC/seed-generation) covers making one.

## Developers

[CLAUDE.md](CLAUDE.md) for the rules, [docs/](docs/) for design notes,
[VERSIONING.md](VERSIONING.md) for what a version number means.

```bash
dotnet test
```

---

*Collaboration by Claude*
