# dice to seed

Turns dice rolls into a BIP-39 seed phrase, offline. It can also turn a separate roll log into
the key that encrypts your backup.

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

**How many rolls.** The vendor minimums are 50 for twelve words and 99 for twenty-four, and the
app keeps them so a seed already made that way can be reproduced. For a **new** seed, roll **60**
or **111**:

| rolls | fair | if one face came up a fifth of the time | target |
| --- | --- | --- | --- |
| 50 | 129.2 | 116.1 | 128 |
| 60 | 155.1 | 139.3 | 128 |
| 99 | 255.9 | 229.9 | 256 |
| 111 | 286.9 | 257.7 | 256 |

A fair d6 carries log2(6) = 2.585 bits, so **99 rolls does not reach 256 bits even with a perfect
die.** It misses by a tenth of a bit. That is a vendor rounding rather than a sufficiency proof,
and it is worth knowing before you rely on the minimum.

Which dice: casino-grade ones have square edges and pips backfilled to the same density as the
body. Ordinary dice have rounded corners and recessed pips, which leaves the 6 face lighter
than the 1. That bias costs about half a bit of average entropy over a whole run, so ordinary
dice are usable; on the conservative measure in the third column it costs more, which is what
the extra rolls are for.
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

Copy it to the stick. On Tails, no terminal is needed:

1. Open the stick in Files and copy the AppImage into your Home folder. Copy it off the stick
   rather than running it in place: a USB stick is often mounted so that programs on it cannot
   run at all.
2. Right-click it, choose **Properties**, and turn on **Executable as Program**.
3. Right-click it again and choose **Run as a Program**.

Step 2 is needed because the permission does not survive the copy from a Windows-formatted
stick, and without it step 3 does not appear. Double-clicking on its own does nothing.

Roll one die at a time and press the matching face. Keys 1 to 6 work, and Backspace undoes the
last roll. The counter targets the recommended count, 60 or 111; Derive unlocks earlier at the
vendor minimum and says so. Watch the counter either way: a log one roll short still produces a
valid-looking seed phrase. Write the words on paper.

Full instructions, including how to check the result against other tools:
[TAILS_INSTRUCTIONS.md](TAILS_INSTRUCTIONS.md).

## The backup key

If you back your seed up with
[slip39-backup](https://github.com/PeteSparrowBTC/slip39-backup), it encrypts the seed with a
32-byte key and splits only that key into shares. Normally that key comes entirely from the
computer's random number generator. This app can take it off dice instead.

Switch to **Rolling for a backup key**. It shows 32 bytes of hex and a four-character check code,
never words, and you type both into the backup tool. Because the key is the SHA-256 of your rolls
and nothing else, `printf '%s' "$ROLLS" | sha256sum` reproduces it, so you can confirm the tool
used the dice you rolled rather than taking its word for it.

**Roll a fresh log.** Never the one behind your seed phrase. The key is the SHA-256 of your rolls,
and a 24-word seed's entropy is that same hash, so one log used twice makes your backup key
derivable from the wallet it is protecting. Switching mode in the app clears your rolls for that
reason. Roll 60 or 111, matching your seed.

It is optional, and worth being plain about the limit. This is the key that wraps your backup, not
the file key inside the age format, which the backup tool generates itself and the payload is
encrypted under. So it is not a claim that no generator is involved anywhere.

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
