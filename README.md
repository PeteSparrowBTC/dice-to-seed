# dice to seed

Turns dice rolls into a BIP-39 seed phrase, offline.

Demo: **https://petesparrowbtc.github.io/dice-to-seed/**

Not for a real seed. The rolls you type are the seed, and from a web page you cannot confirm
the files you were served are the ones in this repository.

## What it is for

You trusted software on your computer, or a hardware wallet, to generate your seed. How do you
know it was safe? And if you gave it dice rolls, how do you know it used them?

You cannot tell by looking. A seed from a broken random number generator looks like any other.
In July 2026 Coldcard firmware was found to have skipped its hardware generator for five years,
and Coinkite said seeds made with at least 50 dice rolls were unaffected.

Dice give you randomness you can account for. Use this to turn them into words, then enter the
same rolls into a second tool and compare.

Either order works. The conversion is deterministic, so any correct implementation produces the
same words from the same rolls, and this app has no random number generator of its own to
distrust. What matters is that two independent tools agree, not which one you ran first.

What decides whether this is safe is the machine, not the tool. Run it offline on Tails, which
keeps nothing, and write the words on paper.

## What you need

- Dice and paper
- Tails, with the network off
- A USB stick

Which dice: casino-grade ones have square edges and pips backfilled to the same density as the
body. Ordinary dice have rounded corners and recessed pips, which leaves the 6 face lighter
than the 1. That bias is real and smaller than people expect, costing a fraction of a bit over
a whole run, so ordinary dice are usable.
[seed-generation has the numbers](https://github.com/PeteSparrowBTC/seed-generation/blob/main/docs/dice.md#your-dice-bias-and-what-it-costs)
and [how to test a die you are unsure of](https://github.com/PeteSparrowBTC/seed-generation/blob/main/docs/dice.md#testing-your-own-dice).

If you test dice, do it before the ceremony on rolls you throw away. Testing the rolls you are
about to use, and re-rolling when they fail, narrows the set your seed comes from.

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
