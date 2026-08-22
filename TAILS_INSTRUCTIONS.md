# Running dice-to-seed on Tails

This is the intended way to run this app. Everything else is a compromise.

The person following this is standing at an offline machine with no other documentation
available, so every step is here rather than referenced.

## What you need

Tails is amnesic and you will be running it with the network off, so **you cannot install
anything.** Everything has to be on the stick before you boot. Assemble this list on a
networked machine first:

- A USB stick with the AppImage on it, downloaded and checked **before** you boot Tails
- **A printed roll sheet**, from `printable/roll-sheet.html` in the repository. Open it in any
  browser and print one page per log you are going to make. It has to be printed beforehand:
  Tails is amnesic and arranging a printer while holding a seed is not a thing you want to be
  doing. The sheet is blank, so it carries nothing until you write on it.
- Tails, booted, with networking off
- Your dice, and paper

For cross-checking, carry at least one of these as well. Both are single files or folders that
run in place, with nothing to install:

- **`rolls12.py`**, from Coldcard's verification docs. One file, imports only `hashlib`,
  carries the wordlist inside it. The most portable check there is.
- **The `mnemonic` package, unzipped.** On a networked machine:
  `pip3 download mnemonic --no-deps -d lib && cd lib && unzip mnemonic-*.whl`. Copy the `lib`
  folder to the stick. It is pure Python, so `PYTHONPATH=lib python3 ...` runs it without
  installing anything.
- **Ian Coleman's `bip39-standalone.html`**, which is one file and opens from `file://`. It has
  two settings traps, and both are silent. Set **Mnemonic Length to 12 or 24, never "raw"**, and
  set the entropy type to **Base 10, not "Dice"**: the Dice type rewrites every 6 to a 0 before
  hashing, so it gives a different wallet with no warning. Test your settings on a log that
  contains at least one 6, because the two types agree on any log without one, and a log of all
  1s therefore proves nothing about the setting that matters.

  You can see this rather than trust it. Tick **Show entropy details** and watch the **Filtered
  Entropy** line, which is the string his tool actually uses: under Base 10 it matches what you
  typed, and under Dice every 6 shows as a `0`. His conversion is not wrong, it is answering a
  different question. Treating rolls as base-6 digits needs the values 0 to 5, and 6 is congruent
  to 0. The vendors do not treat d6 rolls as base-6 digits; they hash the literal characters.

SeedSigner's own `tools/mnemonic.py` needs `pip install` of it and of `embit`, so it is a
check to run before the ceremony on a networked machine, not on Tails.

## Running it

### If someone else will be doing the ceremony, take the zip

`dice-to-seed-<version>-tails.zip` on the
[releases page](https://github.com/PeteSparrowBTC/dice-to-seed/releases) holds the AppImage, its
`SHA256SUMS`, `READ-THIS-FIRST.txt` written for someone who does not use a terminal, and
`start-here.sh`, which checks the app against its fingerprint and refuses to open it if they
disagree. Copy the zip to the stick as it is.

At the Tails machine: extract it in the Files window, drag the extracted folder into Home, then
right-click `start-here.sh` and choose **Run as a Program**. Not a double-click: GNOME Files opens an
executable script in the text editor instead of running it. If "Run as a Program" is missing from the
menu, the file needs Properties and "Executable as Program" first.

The AppImage inside the zip is **deliberately not executable**, so clicking it directly does nothing.
Archive Manager restores the modes the zip stores, confirmed on a real session, which means an
AppImage shipped as executable would open on a double-click and the quickest route into the app would
be the one that skips the check. `start-here.sh` sets the bit itself once the hash matches.

This exists because the verification the rest of this document asks for was impossible to perform
where it matters. Offline there is no release page to read and no documentation on the stick, so
"check the SHA-256" was an instruction nobody at that machine could follow.

Its honest limit, which `READ-THIS-FIRST.txt` states in as many words: the fingerprint travels in
the same folder as the app, so the check proves the file was not damaged or altered, and cannot
prove the download was genuine. Whoever prepared the stick is who is being trusted for that. Do
the build log cross-check below yourself, before you hand the stick over.

### If you are doing it yourself

One file, no server, no port, no browser to configure. Download the AppImage from the
[releases page](https://github.com/PeteSparrowBTC/dice-to-seed/releases) along with
`SHA256SUMS`, check it, and put it on the stick:

```bash
sha256sum -c SHA256SUMS            # both artifacts
sha256sum -c --ignore-missing SHA256SUMS   # if you took only one of them
```

`SHA256SUMS` covers the AppImage and the zip, so `--ignore-missing` is the form to use when you
downloaded one and not the other. Without it, the file you deliberately did not fetch is reported as
a failure.

**Tails does not make that check redundant, and the reason is specific to this app.** Tails decides
whether your seed can get out: it keeps nothing, it runs in RAM, and with the network off there is
nowhere for anything to go. The checksum decides something else, which is whether the program
deriving your seed is the one this project published. A tampered build of this app does not need a
network to hurt you. It only needs to show you twelve words its author can also compute, and you
would then write them on paper and fund them. An offline amnesic session runs that faithfully and
forgets it perfectly.

What the check does prove: the file is intact, the download was not truncated, and nothing altered
it on the stick afterwards.

What it does not prove, said plainly because the opposite is easy to assume: the AppImage and
`SHA256SUMS` come from the same page over the same connection, so anyone able to substitute one can
substitute the other. There is no signature. To get past that you need the hash from somewhere else:
the build log of the tagged release run is public and prints the hash at the moment the file was
made, from a commit you can read. Comparing against that costs a minute and is a different thing to
have to compromise.

On Tails, no terminal is needed:

1. Open the stick in **Files** and copy the AppImage into your **Home** folder. Copy it off the
   stick rather than running it there: removable media is often mounted `noexec`, which stops
   anything on it from running at all.
2. Right-click it, choose **Properties**, and turn on **Executable as Program**.
3. Right-click it again and choose **Run as a Program**.

Step 2 exists because the executable bit does not survive a FAT or exFAT filesystem, which is
what most sticks are formatted as. Without it, **Run as a Program** does not appear in the menu
at all, and a plain double-click does nothing and says nothing.

If you would rather use a terminal:

```bash
cp /media/amnesia/<YOUR_USB>/dice-to-seed-*-x86_64.AppImage ~/
cd ~ && chmod +x dice-to-seed-*-x86_64.AppImage
./dice-to-seed-*-x86_64.AppImage
```

The terminal is worth it once, the first time, because anything that goes wrong is printed
there instead of being swallowed.

It needs nothing installed. Everything it links against ships with Tails: WebKitGTK 4.1, GTK 3,
libsoup 3 and librsvg, all confirmed in the Tails package manifest. See
[docs/tails-platform-notes.md](docs/tails-platform-notes.md).

That is the whole of it. There is no browser to install, no port to check and no proxy setting
to diagnose.

## Using it

0. **Any ordinary die is good enough. Look at the die, not at your rolls.**

   Manufacturing bias in normal dice is real, understood and tiny. A pipped die has its spots
   drilled out and filled with lighter paint, so the six face is missing the most mass and lands
   upward slightly more often. Weldon measured it in 1894 over 315,672 rolls: a 5 or a 6 came up
   33.77% of the time against an expected 33.33%. Across a 60-roll log that costs you 0.004 bits
   on the average measure, or 1.1 bits on the min-entropy floor, out of 155 collected against a
   target of 128. You cannot make an ordinary die matter here.

   What can matter is a die that is loaded or damaged, and that is a question about the object
   rather than about statistics. Use a different die if it is not yours, if an edge or corner is
   chipped, rounded or worn, if one face is scuffed more than the others, if it rocks on a flat
   surface, if it is translucent with a bubble or a filled hole, or if it is hollow, foam, wood,
   oversized or a giveaway. A new die costs less than testing an old one.

   **The salt water test, and whether it will work on your die.** Floating a die in saturated brine
   and spinning it shows up a grossly weighted one: the weight settles to the bottom, so the same
   face keeps surfacing. Whether it floats at all is decided by the plastic, and the margin is
   thinner than the recipe usually admits:

   | | density | in brine at 1.20 g/cm³ |
   | --- | --- | --- |
   | cheap opaque board-game die, ABS or polystyrene | 1.02 to 1.05 | floats easily |
   | translucent acrylic die, the usual RPG dice | 1.19 | floats by 1%, mostly submerged, settles slowly and ambiguously |
   | casino precision die, cellulose acetate | heavier | sinks |

   Saturated is about 26% salt by weight, which is 357 g per litre of water: more than most people
   expect, and the reason "keep adding salt" is in every set of instructions. The published comparison
   that managed to float only 4 dice out of 22 was testing acrylic d20s, which is precisely the
   marginal row. For a cheap d6 the test usually works.

   What it does not do is measure which face lands upward. It tells you where the mass sits, which is
   related but not the same, so treat it as a way to catch a weight glued into a novelty die rather
   than as a fairness measurement.

   **And counting your own rolls cannot substitute for looking at the die.** Over 60 rolls, a face
   that comes up one time in five is expected 12 times against a fair 10. The standard deviation of
   that count is 2.9, so the excess of 2 is 0.7 deviations: smaller than the noise it has to be seen
   against. Three deviations clear would take 1,125 rolls, and for the bias real dice actually have,
   262,223. Weldon's dataset is 315,672 rolls, which is not a coincidence: the only solid measurement
   of ordinary dice bias comes from somebody throwing dice a quarter of a million times because that
   is what the measurement costs.

### How to throw it

The app cannot do this part for you, so it is worth getting right once.

1. **A hard flat surface, and make the die hit something.** A table rather than carpet, which
   absorbs the bounce and lets a die settle without turning over. Throw it against a book stood on
   edge, the inside of a box lid, or the wall behind the table. Casino craps requires both dice to
   strike the far wall, and that wall is lined with rubber pyramids, for exactly this reason: the
   bounce destroys whatever the thrower set up. It is the one dice convention designed to make a
   throw less controllable, so copy it.
2. **The test is tumbling, not a height in centimetres.** The die should turn over several times and
   bounce at least once. What defeats a throw is sliding it, spinning it flat like a top, or dropping
   it from an inch: any of those can carry the starting face straight through. Simplest reliable
   method: shake it in an opaque cup and tip it out, which guarantees the tumbling and means you
   cannot see the die before it lands.
3. **Fix the off-the-table rule before the first throw.** If the die leaves the surface, lands leaning
   against something rather than flat, or cannot be read cleanly, that throw does not count: throw
   again and record only the second one.

   Decide it in advance because the danger is not the re-throw. A die that ends up on the floor is
   not connected to the number it would have shown, so discarding it costs nothing. Choosing to
   discard a roll *after* reading a number you did not like is a different act, and it narrows the set
   your seed came from in the same way re-rolling a whole log does. Apply the rule before you read the
   face, and never discard a roll you have already recorded.
4. **Press the button after each throw, before the next one**, so the counter and your throws cannot
   drift apart. A log one roll short still produces a perfectly convincing seed phrase, for a
   different wallet.

   Two things matter more than which die it is. **How you throw it**, which is the section below,
   and **using one die**, thrown repeatedly, so the order is the order you threw them in.

   An earlier version of this said to rotate several dice, so that a bad one touched only its share of
   the log. That was mitigating the wrong risk. It hedged manufacturing bias, worth about a bit in a
   hundred and fifty-five by the measurement above, and introduced an ordering question worth a third
   of the log. **Five identical dice landed in a heap hold 12.9 bits only if you can say which die is
   which.** If you cannot, what you recorded is the set of faces rather than a sequence, and there are
   252 of those: 8.0 bits, a 38% loss. Sixty rolls thrown that way carry 95.7 bits against a target of
   128, while the counter reads 60 and the words look exactly as convincing. Nothing can detect it
   afterwards.

   Throwing a handful at once is legitimate and faster, if the order is real: use dice you can tell
   apart, read them in a fixed order every time, and never sort them into ascending order, which is
   the same mistake performed on purpose.

   Do not run a statistical test on the rolls you are about to use. At 60 rolls it would miss a
   real 20% bias most of the time while failing one honest log in twenty, and re-rolling a log
   because it failed a test means your seed is drawn from a smaller set than the dice offered,
   which genuinely weakens it. If you want the arithmetic, the chi-squared worksheet in the
   `seed-generation` repository is built for testing a die beforehand, on rolls you throw away.
1. Roll one die, repeatedly. **Sixty rolls for twelve words, a hundred and eleven for twenty-four**, if
   this is a new seed. The vendor minimums of 50 and 99 still derive, because a seed already made
   with 50 rolls has to be reproducible here, but they are thinner than they look: 50 fair rolls
   carry 129.2 bits against 128, and **99 fair rolls carry 255.9 against 256, so the 24-word
   minimum does not reach its target even with a perfect die.** The extra rolls cost minutes and
   settle it. Roll them one at a time and write each result on the printed sheet as you go, because
   that written record is the only thing that can catch a mis-press: every other check in this
   document compares your log against a second implementation of the same conversion, and two tools
   given the same wrong log agree perfectly. Whatever comes up is
   your log: never re-roll it because it looks too orderly. Six 6s in a row is exactly as likely
   as any other six rolls.
2. Record each roll in the app by pressing the button showing the face you rolled. The keys 1
   to 6 do the same thing, Backspace undoes the last roll, and there is an Undo and a Clear.
   There is no text box: nothing but a die face can go in, so a stray keystroke cannot end up
   in your log. Nothing in the app ever picks a value; every roll comes off your dice.
3. Watch the roll counter. It is the largest thing on the page because miscounting the log is
   the most common error in the whole ceremony, and a log one roll short still produces a
   perfectly plausible seed phrase.
4. Read the preimage the app shows. It is the exact string being hashed. This is the value you
   compare against any other tool.
5. **Compare the sheet against the screen, before you press Derive.** The app shows your rolls in
   rows of ten, numbered the same as the rows on the sheet, so this is a row-by-row read rather than
   a hunt through sixty digits. It is the step the sheet exists for: skip it and you have paid the
   cost of a second plaintext copy of your seed and collected none of the benefit.
6. Press Derive, and write the words on paper. There is no copy button, and that is friction
   rather than a safeguard: any text on the page can be selected and copied with the keyboard,
   so the absence of a button prevents nothing. What the app does guarantee is that it never
   writes to the clipboard itself, so nothing lands there unless you put it there. On an offline
   Tails session, where there is no swap and RAM is wiped at shutdown, a clipboard is not the
   thing to worry about; on a machine that is not Tails, the roll log was already exposed.

## If you are also rolling for the backup key

Do this only after the seed is finished and off the screen.

1. Switch the app to **Rolling for a backup key**. If you still have rolls recorded it will ask
   to clear them, and you should let it: a single roll log must never produce both.
2. Roll a completely new log, the same length as the one you used for the seed.
3. Press Derive. You get 32 bytes of hex in numbered groups, and a four-character check code.
   There are no words here and there never will be: hex cannot be mistaken for a seed phrase.
4. Type both into `slip39-backup`. It recomputes the check code and refuses if they disagree,
   which confirms you transcribed the value you rolled rather than one from an earlier session.
5. Destroy the roll log and any paper you wrote the key on. Both are the key in plain text, and a
   key that survives on paper defeats the shares entirely.

Verify it the same way as the seed, since the key is just the hash:

```bash
printf '%s' "$ROLLS" | sha256sum            # the key
printf '%s' "$K_HEX" | sha256sum | cut -c1-4  # the check code
```

Rolling for the key is optional, and here is the limit. This is the key that wraps your backup, not
the file key inside the age format, which `slip39-backup` generates itself and which the payload is
actually encrypted under. So it is not a claim that no generator is involved anywhere.

## Checking the result against something else

Whichever tool you derived with, a second one must agree. The conversion is deterministic, so
any correct implementation gives the same words from the same rolls. In another Tails terminal:

```bash
ROLLS=<your roll log, digits only>
printf '%s' "$ROLLS" | sha256sum
printf '%s' "$ROLLS" | openssl sha256
```

`printf '%s'`, never `echo`. A plain `echo` appends a newline, hashes a different string, and
gives a different wallet. Both commands must print the same hash the app shows.

If you carried `rolls12.py`:

```bash
echo "$ROLLS" | python3 rolls12.py
```

It prints the truncated hash and the twelve words, and they must match the app.

If you carried the unzipped `mnemonic` package, which is an implementation neither this app's
author nor any hardware wallet vendor wrote:

```bash
PYTHONPATH=lib python3 -c "
import hashlib
from mnemonic import Mnemonic
e = hashlib.sha256('$ROLLS'.encode()).digest()[:16]   # 32 for 24 words
print(e.hex())
print(Mnemonic('english').to_mnemonic(e))"
```

Both must agree with the app, character for character. Two independent implementations
agreeing is the whole point of doing this.

On the Coldcard itself: firmware 5.6.0 or later, **12 Word Dice Roll**, **Dice Only** rather
than TRNG plus Dice. The mixed mode blends device entropy that cannot be reproduced, so this
app will legitimately disagree and it will look like a bug. Enter exactly the minimum number
of rolls: below it the device tops up from its own generator without making that obvious,
which hands the provenance back to the device you are trying to check.

## When you are done

Close the app and shut Tails down. It is RAM-only, so everything goes with it, including the
copy of the AppImage you put in your Home folder. Your seed exists on paper and nowhere else.

Destroy the roll log. It is the seed in plain text, before any hashing.

## What this app never does

No random number generator, so it can never invent a roll for you: a test fails the build if
one is ever added to the source. No storage, no cookies, no network call, no telemetry, no
clipboard writes. No BIP-32, no addresses, no fingerprints. It converts the entropy you
brought and shows its working.

---

*Collaboration by Claude*
