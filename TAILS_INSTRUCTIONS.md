# Running dice-to-seed on Tails

This is the intended way to run this app. Everything else is a compromise.

The person following this is standing at an offline machine with no other documentation
available, so every step is here rather than referenced.

## What you need

Tails is amnesic and you will be running it with the network off, so **you cannot install
anything.** Everything has to be on the stick before you boot. Assemble this list on a
networked machine first:

- A USB stick with the published app on it (see "Publishing", below)
- The LibreWolf AppImage on the same stick, downloaded **before** you boot Tails
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
- **Ian Coleman's `bip39-standalone.html`**, which is one file and opens from `file://`.

SeedSigner's own `tools/mnemonic.py` needs `pip install` of it and of `embit`, so it is a
check to run before the ceremony on a networked machine, not on Tails.

## Publishing (on an ordinary machine, with the .NET SDK)

```bash
cd DiceToSeed.Web
dotnet publish -c Release -o publish
```

The app is then `publish/wwwroot`. Copy that folder to the USB stick and name it
`dice-to-seed`. It is entirely static files: no server, no runtime to install, nothing to
configure.

Download the LibreWolf AppImage from librewolf.net and put it on the stick as well. Do this
now, because on Tails you will have networking off and cannot fetch it.

## The short way: the AppImage

One file, no server, no port, no browser to configure. Download it from the latest CI run or
release, verify it, and put it on the stick:

```bash
sha256sum -c dice-to-seed-x86_64.AppImage.sha256
```

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
cp /media/amnesia/<YOUR_USB>/dice-to-seed-x86_64.AppImage ~/
cd ~ && chmod +x dice-to-seed-x86_64.AppImage
./dice-to-seed-x86_64.AppImage
```

The terminal is worth it once, the first time, because anything that goes wrong is printed
there instead of being swallowed.

It needs nothing installed. Everything it links against ships with Tails: WebKitGTK 4.1, GTK 3,
libsoup 3 and librsvg, all confirmed in the Tails package manifest. See
[docs/tails-platform-notes.md](docs/tails-platform-notes.md).

The rest of this document is the browser route, which does the same thing with a local web
server. Both run the identical app; the AppImage merely carries a window with it.

## The browser route (on Tails, networking off)

Open a terminal.

```bash
cd /media/amnesia/<YOUR_USB>/dice-to-seed
chmod +x start-server.sh
./start-server.sh
```

That runs `python3 -m http.server 9876 --bind 127.0.0.1`. Confirm nothing is listening
anywhere else:

```bash
ss -tlnp | grep 9876
```

Expect `127.0.0.1:9876` and nothing else. If you see `0.0.0.0:9876`, stop: the `--bind` did
not take effect and the machine you are about to type a seed into is answering the network.

In another terminal, start the browser:

```bash
cd /media/amnesia/<YOUR_USB>
chmod +x LibreWolf.x86_64.AppImage
./LibreWolf.x86_64.AppImage
```

Go to `http://127.0.0.1:9876`.

### Why LibreWolf and not Tor Browser

Tor Browser on Tails routes `127.0.0.1` through the Tor proxy, so the local connection is
refused and the page never loads. It can be fixed (`about:preferences`, Network Settings,
"No Proxy for", add `127.0.0.1, localhost`) but the setting does not reliably survive, and
diagnosing a proxy at the point where you are about to generate a key is a poor use of your
attention. LibreWolf works with loopback out of the box.

### Why a web server at all

Blazor WebAssembly does not load over `file://`. The browser needs real HTTP for the wasm
MIME type, for module loading and for streaming compilation. Opening `index.html` directly
will fail, and that is not something to work around.

## Using it

0. If you want to know whether your dice are fair, test them **before** you start, with a few
   hundred rolls that you then throw away. Do not test the rolls you are about to use: at 50
   rolls the test barely works, and re-rolling a log because it failed means your seed is
   chosen from a smaller set than the dice offered, which weakens it. The chi-squared
   worksheet in the `seed-generation` repository is built for this.
1. Roll your dice. **Fifty rolls for twelve words, ninety-nine for twenty-four.** Roll them
   one at a time and write each result down as you go. Whatever comes up is your log: never
   re-roll it because it looks too orderly. Six 6s in a row is exactly as likely as any other
   six rolls.
2. Record each roll in the app by pressing the button showing the face you rolled. The keys 1
   to 6 do the same thing, Backspace undoes the last roll, and there is an Undo and a Clear.
   There is no text box: nothing but a die face can go in, so a stray keystroke cannot end up
   in your log. Nothing in the app ever picks a value; every roll comes off your dice.
3. Watch the roll counter. It is the largest thing on the page because miscounting the log is
   the most common error in the whole ceremony, and a log one roll short still produces a
   perfectly plausible seed phrase.
4. Read the preimage the app shows. It is the exact string being hashed. This is the value you
   compare against any other tool.
5. Press Derive, and write the words on paper. There is no copy button and there will not be
   one: on an amnesic offline system a clipboard is a small risk, and on a machine that turns
   out not to be one it is a large risk, and the app cannot tell the difference.

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

Press Ctrl+C in the terminal running the server. Close the browser. Shut Tails down; it is
RAM-only and everything goes with it. Your seed exists on paper and nowhere else.

## What this app never does

No random number generator, so it can never invent a roll for you: a test fails the build if
one is ever added to the source. No storage, no cookies, no network call, no telemetry, no
clipboard writes. No BIP-32, no addresses, no fingerprints. It converts the entropy you
brought and shows its working.

---

*Collaboration by Claude*
