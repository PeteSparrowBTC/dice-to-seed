# Running dice-to-seed on Tails

This is the intended way to run this app. Everything else is a compromise.

The person following this is standing at an offline machine with no other documentation
available, so every step is here rather than referenced.

## What you need

Tails is amnesic and you will be running it with the network off, so **you cannot install
anything.** Everything has to be on the stick before you boot. Assemble this list on a
networked machine first:

- A USB stick with the AppImage on it, downloaded and checked **before** you boot Tails
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

One file, no server, no port, no browser to configure. Download the AppImage from the
[releases page](https://github.com/PeteSparrowBTC/dice-to-seed/releases) along with
`SHA256SUMS`, check it, and put it on the stick:

```bash
sha256sum -c SHA256SUMS
```

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

   If you do want to test one, do it physically: dissolve salt in warm water until a die floats,
   spin it with a finger a dozen times, and see whether the same face keeps coming up on top. A
   weighted die will, because the weight settles to the bottom. That takes a minute and finds the
   bias that matters.

   Two things matter more than the die. Throw it so it **tumbles**, since sliding it or dropping it
   flat from an inch can carry the starting face through; and rotate several dice if you have them,
   so a bad one touches only its share of the log.

   Do not run a statistical test on the rolls you are about to use. At 60 rolls it would miss a
   real 20% bias most of the time while failing one honest log in twenty, and re-rolling a log
   because it failed a test means your seed is drawn from a smaller set than the dice offered,
   which genuinely weakens it. If you want the arithmetic, the chi-squared worksheet in the
   `seed-generation` repository is built for testing a die beforehand, on rolls you throw away.
1. Roll your dice. **Sixty rolls for twelve words, a hundred and eleven for twenty-four**, if
   this is a new seed. The vendor minimums of 50 and 99 still derive, because a seed already made
   with 50 rolls has to be reproducible here, but they are thinner than they look: 50 fair rolls
   carry 129.2 bits against 128, and **99 fair rolls carry 255.9 against 256, so the 24-word
   minimum does not reach its target even with a perfect die.** The extra rolls cost minutes and
   settle it. Roll them one at a time and write each result down as you go. Whatever comes up is
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
5. Press Derive, and write the words on paper. There is no copy button, and that is friction
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
