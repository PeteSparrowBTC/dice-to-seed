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

| rolls | fair | real dice, measured | if one face came up a fifth of the time | target |
| --- | --- | --- | --- | --- |
| 50 | 129.2 | 128.3 | 116.1 | 128 |
| 60 | 155.1 | 154.0 | 139.3 | 128 |
| 99 | 255.9 | 254.1 | 229.9 | 256 |
| 111 | 286.9 | 284.8 | 257.7 | 256 |

A fair d6 carries log2(6) = 2.585 bits, so **99 rolls does not reach 256 bits even with a perfect
die.** It misses by a tenth of a bit. That is a vendor rounding rather than a sufficiency proof,
and it is worth knowing before you rely on the minimum. Note which column the shortfall lives in:
it is the roll count, not the dice.

**Which dice: any ordinary die will do, and here is the size of that claim.** Casino dice have
square edges and pips backfilled to the same density as the body. Ordinary dice have rounded
corners and recessed pips filled with lighter paint, which leaves the 6 face lighter than the 1, so
it lands upward slightly more often. Weldon measured that in 1894 over 315,672 rolls: a 5 or a 6
came up 33.77% of the time against an expected 33.33%, the largest published count on ordinary
dice. Across a 60-roll log it costs 0.004 bits of average entropy, or 1.1 bits on the min-entropy
floor, which is the middle column above. The last column assumes a face at one in five, fifteen
times more lopsided than that measurement, and exists so the recommended counts have margin rather
than because dice are like that.

So the question is provenance, not fairness. A die becomes badly biased because it was made that
way or because it is damaged, and no statistic computed from your rolls will tell you which die
produced them. Use a different die if it is not yours, is chipped, worn or scuffed on one face,
rocks on a flat surface, or is hollow, foam, wood or a giveaway. To test one, float it in
saturated salt water and spin it: a weighted die keeps bringing the same face up.
[seed-generation has the numbers](https://github.com/PeteSparrowBTC/seed-generation/blob/main/docs/dice.md#your-dice-bias-and-what-it-costs)
and [how to test a die you are unsure of](https://github.com/PeteSparrowBTC/seed-generation/blob/main/docs/dice.md#testing-your-own-dice).

Never run the test on the rolls you are about to use. At 60 rolls it would miss a real 20% bias
most of the time while failing one honest log in twenty, and re-rolling because a log failed
narrows the set your seed comes from.

## Use

**If the person doing this is not you, take `dice-to-seed-<version>-tails.zip` instead.** It is the
same app with its fingerprint, instructions for someone who does not use a terminal, and a
`start-here.sh` that checks the app before opening it and refuses if it does not match. Extract it in
the Files window, copy the folder into Home, then right-click `start-here.sh` and choose "Run as a
Program". The AppImage in the zip is deliberately not executable, so the checker is the only way to
start it. Verify the download yourself, as below, before handing the stick over: the check inside the
zip proves the file was not damaged or altered, not that the download was genuine.

Before booting Tails, download from the
[releases page](https://github.com/PeteSparrowBTC/dice-to-seed/releases) and check what you took.
`SHA256SUMS` covers both the AppImage and the zip:

```bash
sha256sum -c SHA256SUMS                    # both
sha256sum -c --ignore-missing SHA256SUMS   # only the one you downloaded
```

Do this even though you will run it on Tails. The two protect different things: Tails decides whether
your seed can get out, and the checksum decides whether the program deriving it is the one published
here. A tampered build needs no network to hurt you, only words its author can also compute, and an
offline session runs it faithfully. The check catches a corrupted download or a stick altered
afterwards; it is not a signature, since the hash file travels with the file it describes, so for
anything stronger compare the hash against the public build log of the tagged release.

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
