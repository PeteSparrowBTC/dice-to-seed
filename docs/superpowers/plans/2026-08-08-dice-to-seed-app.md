# Plan: dice-to-seed, an offline Blazor WebAssembly app

Date: 2026-08-08

This plan is written to be executed by an agent with no prior context. Read it fully before
starting. Every hexadecimal value and every word list in the vectors section was computed and
cross-checked against three independent SHA-256 implementations before this plan was written;
treat them as fixtures, not as illustrations.

## 1. Why this exists

On 31 July 2026 Coinkite shipped emergency firmware for every Coldcard model. Since 2021 the
firmware had not called its hardware true random number generator during seed generation. A
board config set `MICROPY_HW_ENABLE_RNG = 0`, but the guard in `libngu` read
`#ifndef MICROPY_HW_ENABLE_RNG`, which tests only whether the macro is defined and not
whether it is non-zero. `libngu` bound to MicroPython's `rng_get()`, and MicroPython, seeing
the value 0, had compiled a software fallback. Effective seed entropy fell to roughly 40 bits
on Mk3 and roughly 72 bits on Mk4, Mk5 and Q, against an intended 128.

Coinkite's own guidance is the reason this app exists:

> If you added at least 50 fair, independent, private dice rolls when originally creating the
> seed, [we] do not consider that seed at risk from this RNG issue alone.

Dice rolls were the difference between "replace this seed now" and "unaffected", for a defect
that survived five years of audited, reproducibly built, open source firmware. You cannot
audit entropy after the fact: a seed from a broken generator is indistinguishable from a good
one. The only defence is to contribute unpredictability you can account for yourself, and then
to confirm that the device used it.

Confirming that requires a **second implementation**. That is this app's entire job. It is not
the primary generator and it is not a wallet.

The immediate use case: the owner has a Coldcard Mk4 and wants a 12-word seed from 50 d6
rolls, agreed on by three independent implementations (the Mk4, this app, and Ian Coleman's
offline page) before any money touches it.

## 2. Goal

A Blazor WebAssembly app, served from static files, that converts a log of six-sided dice
rolls into a BIP-39 mnemonic using exactly the convention Coldcard and SeedSigner use, and
that displays every intermediate value so each step can be checked against another tool.

It must run with the network physically disconnected, from a USB stick, in a browser on a
Tails session, with no runtime installed and no external resource fetched.

## 3. Non-goals

Each of these is excluded deliberately. Do not add them, and do not treat their absence as an
oversight.

| excluded | why |
| --- | --- |
| BIP-32, secp256k1, master fingerprints, addresses | Needs elliptic curve code, which would be the largest and least reviewable part of the app. Comparing 12 words by eye is sufficient and is what Coldcard's own verification flow does. The fingerprint gap is already covered elsewhere by BitcoinQnA's Seed Tool. |
| SLIP-39 splitting | Belongs to the sibling `slip39-backup` app. Keeping generation and splitting in separate artifacts means a defect in one cannot silently validate the other. |
| Coin flips, cards, d20, d16 tables | The immediate need is d6. Adding sources multiplies the vector surface for no current benefit. Revisit only when asked. |
| Saving, exporting, printing or copying the seed | Every one of those is a path off the airgap. The user writes the words on paper. |
| Dice fairness testing | Covered by the `seed-generation` repository's chi-squared worksheet, and harmful here. At 50 rolls the test has almost no power (5 degrees of freedom, 8.33 expected per face, roughly one honest session in twenty exceeding the 0.05 critical value), and worse, a test on the rolls about to be used invites re-rolling on failure, which conditions the seed on passing a statistical test and removes part of the output space. Fairness is checked before the ceremony, on rolls that are then discarded. The app instead warns against re-rolling a log for looking non-random. |
| A "generate for me" button, a simulated die, or any other random value | The app must never be a source of entropy. It converts entropy the user brought on physical dice. This is not a preference to be traded off later: see section 3a, and CLAUDE.md rule 1, which is enforced by a test that fails the build. |
| Serving the app from `file://` | Blazor WebAssembly does not load over `file://`. A local HTTP server is not an inconvenience to be engineered away, it is the only way the app runs. See section 10. |

## 3a. The no-entropy rule, and how it is enforced

The name `dice-to-seed` invites a "roll for me" button, and a browser is the worst available
place to make a key: an unauditable RNG, on a general-purpose machine, in a process that can
be reached by anything else the page loads. The Coldcard defect in section 1 is the argument.
A seed from a broken generator is indistinguishable from a good one, which is why the only
defence is entropy the user produced physically and can account for.

So the app has no RNG, and the rule is enforced rather than documented. `DiceToSeed.Tests`
carries a guard test that reads the first-party source and fails on any occurrence of:

```
RandomNumberGenerator   System.Random   new Random   Guid.NewGuid
crypto.getRandomValues  Math.random     GetNonZeroBytes
```

Scope it to the `.cs`, `.razor`, `.js` and `.css` files under `DiceToSeed.Core` and
`DiceToSeed.Web`, excluding `bin`, `obj` and `wwwroot/_framework`, and excluding the guard
test's own file, which necessarily contains every string it searches for.

Do not point the scan at published output. The .NET WebAssembly runtime calls
`crypto.getRandomValues` on its own account, so a scan over `publish/wwwroot` goes red for a
reason that is not a defect here, and a permanently red check gets disabled. The scan covers
first-party source, which is the thing this repository controls.

## 4. The algorithm, exactly

Given a roll string `R` of ASCII digits `1` to `6` and a target word count `W` in {12, 24}:

1. `entropyBits = 32 * W / 3`. That is 128 for 12 words, 256 for 24.
2. `H = SHA-256(preimage)`, where `preimage` is the ASCII bytes of the roll digits joined by
   nothing at all, with **nothing appended**. No separator, no trailing newline. See section 5:
   all three d6 vendors hash the bare digit string.
3. `entropy = the first (entropyBits / 8) bytes of H`. **Truncate the hash. Do not hash it
   again.** For 24 words this is all 32 bytes; for 12 words it is the first 16.
4. `checksumBits = entropyBits / 32`, which is 8 for 256-bit entropy and 4 for 128-bit.
   `checksum = the top checksumBits of SHA-256(entropy)`.
5. Concatenate `entropy || checksum`, split into 11-bit groups, and use each group as a
   zero-based index into the 2048-word list. 264 bits gives 24 groups; 132 bits gives 12.

Step 5 is ordinary BIP-39. Steps 1 to 3 are the dice convention, and matching them exactly is
what makes cross-verification possible.

The single most common way to get this wrong is to hash the hash at step 3, or to derive the
checksum from `H` rather than from the truncated `entropy`. Vector 4 in section 8 exists
specifically to catch that: it shares its first eleven words with vector 3 and differs only in
the twelfth.

## 5. Dialects, and why the app must show the preimage

**Resolved during implementation, and the answer reverses this section's original premise.**
The draft claimed Krux hashes `1-2-3-4-5-6` where Coldcard hashes `123456`, and required a
separator control to cover both. Reading all three vendors' source shows there is no d6
dialect split:

| vendor | source | what it hashes for d6 |
| --- | --- | --- |
| Coldcard | `docs/rolls12.py` | `sha256(r.encode()).digest()[:16]`, `r` the bare digits |
| SeedSigner | `src/seedsigner/helpers/mnemonic_generation.py` | `hashlib.sha256(roll_data.encode()).digest()`, then `[:16]` |
| Krux | `src/krux/pages/new_mnemonic/dice_rolls.py` | `"".join(self.rolls) if self.num_sides < 10 else "-".join(self.rolls)` |

Krux's dash is its **d20** convention. With twenty faces a value can be two digits, so `1`
followed by `2` has to be distinguishable from `12`; with six faces it cannot happen and Krux
joins with nothing. That line has read the same way since v22.08.2 in 2022. Krux's d6 minimums
are also the same as Coldcard's: 50 rolls for 12 words, 99 for 24.

Confirmed the other way round as well: Coldcard's `rolls12.py`, run against SeedSigner's own
published 50-roll example, prints SeedSigner's published twelve words.

**Consequence: the app has no separator control.** In a d6-only tool the dash setting would
produce a seed that no vendor reproduces, which is the exact failure the app exists to detect.
A control whose only non-default position is wrong is a footgun, not a feature. If d20 is ever
added, the separator returns with it and with its own vectors.

What the original concern got right, and what still holds:

- The app always renders the exact preimage it hashed, in a monospace block, character for
  character. This is the single most important element on the screen. A user comparing against
  another tool must be able to see precisely what was fed to SHA-256. That requirement is
  unchanged and is now the only defence against a preimage disagreement, since there is no
  control to point at when one occurs.
- Vectors are taken from vendors' own published output, at word level, not at hash level, and
  from more than one vendor. A convention that has not been confirmed against a vendor's
  published output is never offered under that vendor's name: an unverified dialect and a
  device that ignored the rolls look identical from the user's chair.

## 6. Architecture

Three projects. The conversion lives in a library with no UI dependency so that it can be read
and tested on its own.

```
dice-to-seed/
  DiceToSeed.Core/            class library, netstandard2.1 or net9.0
    DiceRolls.cs              parsing and validation -> Result
    Bip39.cs                  entropy -> words, and the wordlist
    DiceSeed.cs               the five steps of section 4
    WordList.english.txt      embedded resource
  DiceToSeed.Web/             Blazor WebAssembly, standalone (no server project)
    Pages/Derive.razor        the single page
    wwwroot/                  no external references of any kind
  DiceToSeed.Tests/           xunit, references Core only
    NoEntropySourceTests.cs   the section 3a source scan; excluded from its own scan
  .github/workflows/ci.yml
```

`DiceToSeed.Core` must not reference `Microsoft.AspNetCore.*`. Its only NuGet dependency is
`CSharpFunctionalExtensions`, for `Result`.

That dependency is a deliberate carve-out from the rule that this repository takes no
dependency it cannot verify itself. `CSharpFunctionalExtensions` supplies a `Result` type and
nothing else: no cryptography, no I/O, no network. It is small enough to read, and it sits
outside the derivation path. Nothing else earns the same exemption, and in particular no
library that touches bytes on the way to a key does.

The test project reaches the file system, which Core and Web never do, solely for the section
3a source scan.

### Core public surface

Keep it this small. Records, immutable, read-only collections.

```csharp
public enum WordCount { Twelve = 12, TwentyFour = 24 }
public sealed record SeedDerivation(
    string Preimage,
    string Sha256Hex,
    string EntropyHex,
    IReadOnlyList<string> Words);

public static class DiceSeed
{
    public static Result<SeedDerivation> Derive(string rolls, WordCount words);
}
```

There is no `RollSeparator`. Section 5 records why it was removed rather than kept unused: an
enum on the public surface of a key-generation library is an invitation to put a control in
front of it, and the only value it could carry produces a seed no d6 vendor reproduces.

`Derive` returns a failed `Result` with a specific message, never an exception, for: an empty
log, any character outside `1`-`6`, and a log shorter than the minimum for the requested word
count. Whitespace and line breaks in the input are stripped before validation, so a log typed
in groups of ten is accepted.

Minimums: **50 rolls for 12 words, 99 for 24.** Below the minimum, fail with wording that
names the real problem, along the lines of: "38 rolls is below the 50-roll minimum for 12
words. A short log does not become stronger by producing 12 words; the word count is not the
entropy."

Do not implement a "recommended" count above the minimum. The 50 and 99 figures are the
vendor conventions and this app's job is to agree with the vendors, not to editorialise.

### Wordlist integrity

Embed `english.txt` and verify it at startup against:

```
2f5eed53a4727b4bf8880d8f3f199efc90e58503646d9ff8eff3a2ed3b24dbda
```

If the hash does not match, or the list is not exactly 2048 words, the app refuses to derive
anything and says why. Display this hash in the UI so the user can confirm it independently.
Note `.gitattributes` pins `*.txt` to LF for exactly this reason.

## 7. UI requirements

One page. No routing beyond it, no navigation menu, no settings.

1. **An offline warning at the top**, prominent, in the manner of the sibling `slip39-backup`
   README. If `window.location.hostname` is neither `127.0.0.1` nor `localhost`, escalate it:
   this build is being served from somewhere, and the user must be told plainly, above the
   dice buttons, not to record a real roll log in it. The roll log is the seed in plaintext,
   before any hashing, so a hosted build of this app carries more risk than a hosted build of
   a tool that splits a seed the user already holds.

   Do not add a `file:` branch to that check. Blazor WebAssembly does not load over `file://`
   at all, so the branch can never run; a condition that cannot be true reads as a supported
   path and invites someone to "fix" the app so it works there.
2. **Word count**: 12 (default) and 24.
3. **No separator control.** All three d6 vendors hash the bare digit string; see section 5.
4. **Roll entry: six dice-face buttons, one per face, clicked once per physical roll.** The
   original design here was a text area, and building it showed why that was the wrong shape.
   A free-text box must then answer: what happens to a `7`, to a pasted file, to a keystroke
   filtered out from the middle of a log. Every answer trades one silent failure for another,
   and the page spent more code defending the box than doing the conversion.

   Six buttons remove the whole class rather than guarding it: no character outside 1 to 6 can
   exist, nothing needs filtering, there is no caret to lose, no paste to guard, and the count
   is exactly what was pressed. It also matches the ceremony, which is one physical roll at a
   time. Keys 1 to 6 do the same thing for anyone who would rather not use a mouse, Backspace
   undoes the last roll, and there is an explicit Undo and Clear.

   **The buttons record a roll; they never make one.** A button bearing a die face is exactly
   what a later reader might wire to a random pick, and that one change would turn a verifier
   into a browser key generator. The code carries that warning at the button markup, and
   CLAUDE.md rule 1 is enforced by a test.

   The pips are plain elements on a 3x3 grid, not a dice glyph font, so the faces render the
   same on a system with minimal fonts installed.
5. **A live roll counter, rendered large: `37 / 50`.** Miscounting the log is the most common
   error in the whole ceremony, so make the count impossible to miss. Show clearly when the
   count is under, exactly at, or over the minimum.
6. **The live preimage**, monospace, exactly as it will be hashed.
7. **Derive**, disabled until the input validates.
8. **Output**, all four values, each individually readable: the preimage, the SHA-256 of it,
   the truncated entropy hex, and the numbered words in a list.
9. **A verification panel** carrying the commands and settings from section 9, so the user does
   not need this plan open while running the ceremony.
10. **The wordlist SHA-256**, displayed as a footer.

No copy-to-clipboard button for the words, the entropy or the preimage. On an amnesic offline
system a clipboard is a small risk; on a machine that turns out not to be either one it is a
large one, and the app cannot tell the difference. The user is writing on paper.

No `localStorage`, no `sessionStorage`, no cookies, no service worker caching of derived
values, no external fonts, no CDN, no analytics. The published output must load with the
network disconnected.

## 8. Test vectors

These are fixtures. All were verified against three independent SHA-256 implementations and,
for vectors 1 to 3, against the published sources named.

**Vectors 1 to 3: the official BIP-39 English vectors** (entropy straight to words, bypassing
the dice step; they test `Bip39.cs` alone).

| entropy hex | expected mnemonic |
| --- | --- |
| `00000000000000000000000000000000` | abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about |
| `7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f` | legal winner thank year wave sausage worth useful legal winner thank yellow |
| `80808080808080808080808080808080` | letter advice cage absurd amount doctor acoustic avoid letter advice cage above |

**Vector 4: Coldcard's own published example, 24 words.** Rolls `123456`. This
is the value in Coldcard's verification documentation, so agreement here means agreement with
the vendor.

```
preimage  123456
sha256    8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92
words     mirror reject rookie talk pudding throw happy era myth already payment own
          sentence push head sting video explain letter bomb casual hotel rather garment
```

**Vector 5: the same rolls at 12 words.** This is the truncation test. The first eleven words
are identical to vector 4 and the twelfth differs, which is only true if the entropy is the
truncated hash and the checksum is computed from that truncation. Re-hashing, or taking the
checksum from the full hash, changes the twelfth word and usually the others too.

```
preimage  123456
entropy   8d969eef6ecad3c29a3a629280e686cf
words     mirror reject rookie talk pudding throw happy era myth already payment owner
```

**Vector 6: withdrawn.** It asserted that `1-2-3-4-5-6` is the Krux preimage for those rolls.
The hash was correct for that string (`b76c3b01...f851`, recomputed) but the premise was not:
Krux joins d6 rolls with nothing, as section 5 now records, and reserves the dash for d20. No
d6 vendor hashes a dashed string, so the vector tested a convention that does not exist. What
replaced it is a test that records the finding, with the Krux source line quoted, so the
question is not reopened from memory.

**Vector 7: the primary 12-word case, 50 rolls.** `123456` repeated and cut to 50 digits.

```
preimage  12345612345612345612345612345612345612345612345612
sha256    ee72ae915a4e6ea7ccbeb8e5e5eecef29a1d0d90f053183726a424b6d3b07325
entropy   ee72ae915a4e6ea7ccbeb8e5e5eecef2
words     unveil nice picture region tragic fault cream strike tourist control recipe tourist
```

**Vector 8: the 24-word case, 99 rolls.** `123456` repeated and cut to 99 digits.

```
preimage  123456123456123456123456123456123456123456123456123456123456123456123456
          123456123456123456123456123
sha256    5588d3630bd19f6375b7bd922457af34ea9c74f00807566a1cf808e445dc8c20
entropy   5588d3630bd19f6375b7bd922457af34ea9c74f00807566a1cf808e445dc8c20
words     few educate sugar bless boring random strategy waste mutual cargo type hawk
          prefer denial scan abstract filter extend dignity balcony dust unusual correct bubble
```

**Vectors 9 and 10: SeedSigner's published examples**, from `docs/dice_verification.md` in the
SeedSigner repository. These are the vectors that make the app's premise checkable, because
they come from a second vendor rather than from this plan's own arithmetic.

```
rolls   65515223131652132161133154444123616466443112153441            (50 rolls, 12 words)
sha256  6cb09af855050dcde6fe2adc3181c250982011e2cf17821cbed56a908ec527c3
words   hole luggage safe present express tragic orbit shed switch metal identify path

rolls   655152231316521321611331544441236164664431121534415633526456254462245546236542
        364246312613322234612                                          (99 rolls, 24 words)
sha256  51531761ec7a738946e0b9f46bb11320a695495430e345c14f01ad8b3b898a6d
words   eyebrow obvious such suggest poet seven breeze blame virtual frown dynamic donor
        harsh pigeon express broccoli easy apology scatter force recipe shadow claim radio
```

Vectors 4 to 8 use roll strings far below the minimum, or exactly at it. Expose an internal
entry point for the tests that bypasses the minimum-length check, or make the minimum a
parameter with a default. Do not weaken the check that the UI uses.

**The verification item is closed.** Coldcard's `rolls12.py` was downloaded from
`https://coldcard.com/docs/rolls12.py` and run. It prints, for input `123456`:

```
8d969eef6ecad3c29a3a629280e686cf
   1: mirror ... 11: payment  12: owner
```

which is vector 5, and its source states the rule directly:

```python
h = sha256(r.encode()).digest()[:16]          # truncate, do not re-hash
indexes[-1] += sha256(entropy).digest()[0] >> 4   # checksum from the TRUNCATION
```

The same script, run against SeedSigner's published 50-roll example, prints SeedSigner's
published twelve words. Coldcard and SeedSigner therefore agree with each other, and this
app's suite asserts both vendors' published outputs.

## 9. Cross-verification, the runbook the app must reproduce

This is what the verification panel in the UI should tell the user.

**The hash, with two independent programs:**

```bash
printf '%s' "$ROLLS" | sha256sum
printf '%s' "$ROLLS" | openssl sha256
```

`printf '%s'`, not `echo`. A plain `echo` appends a newline, hashes a different string, and
yields a different wallet.

**Ian Coleman's offline page**, `bip39-standalone.html`. Two settings matter and both are
silent footguns:

1. Set **Mnemonic Length to 12** (or 24), **not "raw"**. Only the numeric lengths take the
   SHA-256 path; "raw" uses a lossy direct base-6 mapping worth about 1.67 bits per roll
   instead of 2.585.
2. Set the entropy type to **Base 10**, **not "Dice"**. The "Dice" type rewrites every `6` to
   `0` before hashing, so identical digits give a different seed. From `entropy.js`:
   `// Convert dice to base6 entropy (ie 1-6 to 0-5) / This is done by changing all 6s to 0s`.
   "Base 10" leaves the digits intact, so the hashed string is the one `sha256sum` sees.

**Coldcard Mk4:**

- Update to firmware 5.6.0 or later first. Dice Only mode does not use the RNG, so a
  dice-derived seed is not at risk from the July 2026 defect, but there is no reason to run
  known-bad firmware.
- Choose **12 Word Dice Roll** and use **Dice Only**, not TRNG plus Dice. The mixed mode
  blends device entropy that cannot be reproduced, so this app will legitimately disagree and
  it will look like a bug.
- Enter **exactly 50 rolls**. Below the minimum the device tops up from its own generator
  without making that obvious, which hands the provenance back to the device.
- The device displays the running SHA-256 as rolls are entered. The zero-roll screen shows
  `e3b0c442...b855`, which is SHA-256 of the empty string, and is a free confirmation that the
  hash display works before any real digit is entered.

**Do not enter a real roll log on a networked general-purpose machine.** Coldcard's own
documentation states that doing so completely compromises the device's security.

## 10. Deployment posture: Tails first

The sibling `slip39-backup` has a two-build posture that is known to work, and this app
should inherit it, with one deliberate difference.

**What `slip39-backup` does.** A GitHub Pages build carries a red banner at the top of the
README and of the page: live demo, do not use for real wallets, download a release and run it
offline on Tails instead. `TAILS_INSTRUCTIONS.md` then covers the whole offline route: publish
to static files, copy to USB, `python3 -m http.server 9876 --bind 127.0.0.1`, open in
LibreWolf, and an explicit section on why `file://` cannot work. The Tails route in that
repository has been run and works.

**The difference here.** `slip39-backup` splits a seed the user already holds, so a demo can
be exercised with a throwaway seed and still teach the mechanism. This app's input is the
roll log, which **is** the seed in plaintext before any hashing, and the only way to see the
app do anything is to type one in. A demo build therefore teaches very little and asks for
exactly the input that must never be typed on a networked machine. Recommendation: **no
GitHub Pages demo**. Distribute the published `wwwroot` as a release artifact only, and keep
the banner logic anyway for the case where someone serves it themselves. Section 13 records
this as the human's call rather than settling it here.

**Publish, on a machine with the .NET SDK:**

```bash
cd DiceToSeed.Web
dotnet publish -c Release -o publish
# copy publish/wwwroot to the USB stick, as a folder named dice-to-seed
```

**Run, on Tails, with networking off:**

```bash
cd /media/amnesia/<USB>/dice-to-seed
./start-server.sh            # or: python3 -m http.server 9876 --bind 127.0.0.1
```

Then open `http://127.0.0.1:9876` in LibreWolf. Three points that the README must state
plainly, because each has cost time in the sibling repository:

- **A local web server is required.** Blazor WebAssembly does not load over `file://`: the
  MIME types, module loading and streaming compilation all depend on HTTP. There is no
  double-click route, and this is not a bug to be worked around.
- **LibreWolf, not Tor Browser.** Tor Browser on Tails sends `127.0.0.1` through the Tor
  proxy and the local connection is refused. It can be fixed by adding `127.0.0.1, localhost`
  to "No Proxy for" in `about:preferences`, but the setting does not always survive; carrying
  the LibreWolf AppImage on the USB stick is the shorter path. Copy it to the stick before
  booting Tails, since fetching it there means networking on.
- **`--bind 127.0.0.1` is load-bearing.** Without it Python listens on every interface.
  `ss -tlnp | grep 9876` should show `127.0.0.1:9876` and nothing else.

Ship `start-server.sh` alongside the published output (LF endings, already pinned by
`.gitattributes`), and give this repository its own `TAILS_INSTRUCTIONS.md` modelled on the
sibling's, since the person following it is standing at an offline machine with no other
documentation available.

## 11. CI

`.github/workflows/ci.yml`, on pull requests into main and on `workflow_dispatch`:

1. `dotnet restore`
2. `dotnet build --configuration Release --no-restore`
3. `dotnet test --configuration Release --no-build`
4. `dotnet publish DiceToSeed.Web -c Release` and upload `wwwroot` as a build artifact, so a
   reviewer can download exactly what would go on a USB stick.

Comment the workflow generously: state what each step is for and why the artifact upload
exists.

Do not add an `on: push` workflow that tries to block direct pushes to main. It runs after the
server has accepted the push, so it can only report, never block, and it produces a
permanently red check on legitimate merges. Branch protection is the real enforcement; see
CLAUDE.md.

## 12. Ordered task list

Work test-first. Each step should end with a passing suite and a commit on a feature branch.

1. Scaffold the three projects and the solution. Add `CSharpFunctionalExtensions` to Core and
   xunit to Tests. Confirm `dotnet test` runs green with zero tests.
1a. Write `NoEntropySourceTests.cs`, the section 3a source scan, before any production code.
   Prove it works by temporarily adding `var x = new Random();` to a Core file, watching the
   test go red, then removing it. A guard test that has never failed is not known to work.
2. Add `english.txt` as an embedded resource. Write the integrity test first (2048 words, and
   the SHA-256 in section 6), then make it pass.
3. Implement `Bip39.cs`, entropy bytes to words. Drive it with vectors 1 to 3. This is the part
   most likely to have an off-by-one in the 11-bit split, so write those three tests before any
   implementation.
4. Implement `DiceRolls.cs`: whitespace stripping, character validation, minimum-length rules,
   all returning `Result`. Test the three failure modes and their messages.
5. Implement `DiceSeed.Derive`, the five steps of section 4. Drive it with vectors 4 to 8.
   Vector 5 is the one that catches a wrong truncation, so make sure it is present and passing.
6. **Done.** Coldcard's `rolls12.py` downloaded and run: it prints vector 5, and its source
   states the truncation and checksum rules outright. See section 8.
6a. **Done, and it changed the design.** Krux joins d6 rolls with nothing and uses the dash
   only for d20, unchanged since v22.08.2. There is no d6 dialect split, so the separator
   control is gone from both the UI and the Core API. SeedSigner's two published examples
   were added as vectors 9 and 10 in its place. See section 5.
7. Build the single Blazor page against the Core API. Start with the roll counter and the live
   preimage, because those are the elements that carry the verification value.
8. Add the offline warning and the served-from-elsewhere escalation.
9. Add the verification panel with the section 9 content.
10. Grep the published output for external references (`http://`, `https://`, `cdn`, font
    URLs) and confirm there are none. Load the published app with networking disabled and
    confirm it works.
11. Write `start-server.sh` and `TAILS_INSTRUCTIONS.md` per section 10, then run the whole
    route on a real Tails session: USB, server, LibreWolf, 50 rolls, words on paper. The
    sibling repository's instructions were written this way and that is why they work.
12. Write the README: what it is, that it is Tails first and download only, the three-way
    verification procedure, a pointer to `TAILS_INSTRUCTIONS.md`, and an explicit statement
    that this app is a checker rather than a recommended primary generator.
13. Add the CI workflow.
14. Open a pull request. Do not merge it.

## 13. Decisions left to the human

- **Repository visibility.** Branch protection on the free plan needs a public repository. The
  sibling repositories are public. Not yet created on GitHub; the local repository has no
  remote.
- **Whether the sibling `seed-generation` repository should link to this app.** That repository
  is deliberately a vendor-neutral reference that points at Coldcard, SeedSigner and Coleman.
  Linking to an app from the same author changes it from referee to participant. Worth a
  deliberate decision rather than a drive-by link.
- **Whether to support coin flips later.** SeedSigner accepts 128 or 256 coin flips hashed the
  same way, so it would be a small addition to Core with its own vectors. Out of scope here.
- **Whether to publish a GitHub Pages demo at all.** `slip39-backup` does, behind a red
  banner, and the banner pattern works. Section 10 recommends against it here: the input to
  this app is the seed in plaintext, and the demo cannot be exercised without typing one.
  Release artifact only is the safer default. The banner logic gets built either way, because
  it costs little and someone may serve the folder themselves.

---

*Collaboration by Claude*
