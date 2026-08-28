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

- One die, and paper for the words
- A printed roll sheet, one page per log, printed before you boot. One per word count, so the sheet
  only ever mentions the number of throws you are actually making:
  [twelve words](DiceToSeed.Web/wwwroot/roll-sheet-12-words.pdf) (sixty boxes) or
  [twenty-four](DiceToSeed.Web/wwwroot/roll-sheet-24-words.pdf) (a hundred and eleven). Both are served
  by the app, so each is a link on the demo and a file in the AppImage, and both ship with every
  release; the page links whichever matches the word count you have selected. They are generated from
  the HTML in [`printable/`](printable/), which stays in the repository to be read rather than served.
  The sheets are blank, so they carry nothing until you write on them, and comparing one against the
  screen is the only thing that can catch a mis-press
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
rocks on a flat surface, or is hollow, foam, wood or a giveaway.

To test one, float it in saturated salt water and spin it: a weighted die keeps bringing the same
face up, because the weight settles to the bottom. Whether it floats at all is decided by the
plastic, and the margin is thinner than the recipe admits. Brine saturates at 1.20 g/cm³, about 26%
by weight or 357 g per litre. A cheap opaque board-game die is ABS or polystyrene at 1.02 to 1.05 and
floats easily; a translucent acrylic die is 1.19 and floats by one percent, mostly submerged and
settling ambiguously; a casino die is cellulose acetate and sinks. The published comparison that
floated only 4 dice out of 22 was testing acrylic d20s, the marginal case. It also measures where the
mass sits rather than which face lands upward, so it catches a weight glued into a novelty die and is
not a fairness measurement.
[seed-generation has the numbers](https://github.com/PeteSparrowBTC/seed-generation/blob/main/docs/dice.md#your-dice-bias-and-what-it-costs)
and [how to test a die you are unsure of](https://github.com/PeteSparrowBTC/seed-generation/blob/main/docs/dice.md#testing-your-own-dice).

**Counting rolls cannot substitute for looking at the die.** Over 60 rolls a face that comes up one
time in five is expected 12 times against a fair 10, and the standard deviation of that count is 2.9,
so the excess is 0.7 deviations. Three deviations clear takes 1,125 rolls; for the bias real dice
actually have, 262,223. Weldon's dataset is 315,672 rolls, which is not a coincidence: that is what
the measurement costs.

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

**Use one die, thrown repeatedly.** Then the order is the order you threw them in, and there is
nothing to get wrong. Throwing a handful at once is faster and needs the order to be real: five
identical dice landed in a heap hold 12.9 bits only if you can say which die is which, and if you
cannot, what you recorded is the set of faces rather than a sequence. There are 252 of those, which is
8.0 bits, a 38% loss. A 60-roll log thrown that way carries 95.7 bits against a target of 128, while
the counter reads 60 and the words look exactly as convincing. Nothing in the app can detect it.

So if you do throw several, use dice you can tell apart, read them in a fixed order every time, and
never sort them into ascending order, which is the same mistake performed on purpose.

## How to throw it

The app cannot do this part, so it is the part worth reading once.

**Throw it inside a box, standing on a table.** A shoe box is the right size. The walls give the die
something to bounce off and keep it on the surface, which is the off-the-table case removed rather
than adjudicated, and over a hundred and eleven throws that is worth having. Put the box on a table
rather than on carpet or your lap: a thin base on something soft absorbs the bounce and lets a die
settle without turning over, which is the failure the walls are there to prevent. Casino craps
requires both dice to strike the far wall, and that wall is lined with rubber pyramids, for exactly
this reason: the bounce destroys whatever the thrower set up, which is the property you want and the
reason [dice control](https://en.wikipedia.org/wiki/Dice_control) is hard at a real table. Without a
box, throw against a book stood on edge or the wall.

**The test is tumbling, not a height.** Several turns and at least one bounce. Sliding it, spinning it
flat, or dropping it from an inch can carry the starting face straight through. Room to travel is the
only thing the box has to supply, which is why a shoe box and not a tin: in something too small the
die lands, wobbles and stops on the face it started on. The most reliable method is the box with its
lid on, or an opaque cup: shake and tip out, which guarantees tumbling and means you cannot see the
die before it lands.

**Fix the off-the-table rule before the first throw.** If it leaves the surface, lands leaning rather
than flat, or cannot be read cleanly, that throw does not count: throw again and record the second
one. Decide it in advance, because the danger is not the re-throw. A die on the floor is unconnected to
the number it would have shown, so discarding it costs nothing; choosing to discard a roll after
reading a number you did not like is a different act, and narrows the set your seed came from. Apply
the rule before you read the face.

In a box the leaning clause does more of the work than the off-the-surface one, because there is now a
wall for the die to rest against. That is the trade the box makes, and it is a good one: a leaning die
is in front of you and unmistakable, where a die on the floor is a throw you have to reconstruct.

**Press the button after each throw**, before the next, so the counter and the throws cannot drift
apart. A log one roll short produces a perfectly convincing seed phrase for a different wallet.

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
