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
| Dice fairness testing | Covered by the `seed-generation` repository's chi-squared worksheet. Not this app's concern. |
| A "generate for me" button using a browser RNG | The app must never be a source of entropy. It converts entropy the user brought. |

## 4. The algorithm, exactly

Given a roll string `R` of ASCII digits `1` to `6` and a target word count `W` in {12, 24}:

1. `entropyBits = 32 * W / 3`. That is 128 for 12 words, 256 for 24.
2. `H = SHA-256(preimage)`, where `preimage` is the ASCII bytes of the roll digits joined by
   the chosen separator, with **nothing appended**. No trailing newline.
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

Identical physical rolls produce different seeds under different vendors' conventions, and
nothing on any device's screen announces which one is in use:

| convention | preimage for rolls 1,2,3,4,5,6 |
| --- | --- |
| Coldcard, SeedSigner, Ian Coleman set to Base 10 | `123456` |
| Krux | `1-2-3-4-5-6` |

Those two strings hash to values with nothing in common (see vectors 5 and 6). A verification
that fails because the preimage was assembled the other way looks exactly like a verification
that fails because the device ignored the rolls, and the second is the failure being checked
for.

Therefore:

- The app offers a separator choice: **none** (default) and **dash**.
- The app always renders the exact preimage it hashed, in a monospace block, character for
  character. This is the single most important element on the screen. A user comparing against
  another tool must be able to see precisely what was fed to SHA-256.

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
  .github/workflows/ci.yml
```

`DiceToSeed.Core` must not reference `Microsoft.AspNetCore.*`. Its only NuGet dependency is
`CSharpFunctionalExtensions`, for `Result`.

### Core public surface

Keep it this small. Records, immutable, read-only collections.

```csharp
public enum WordCount { Twelve = 12, TwentyFour = 24 }
public enum RollSeparator { None, Dash }

public sealed record SeedDerivation(
    string Preimage,
    string Sha256Hex,
    string EntropyHex,
    IReadOnlyList<string> Words);

public static class DiceSeed
{
    public static Result<SeedDerivation> Derive(
        string rolls, WordCount words, RollSeparator separator);
}
```

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
   README. If `window.location.hostname` is neither `127.0.0.1` nor `localhost` and the scheme
   is not `file:`, escalate the warning: this build is being served from somewhere, and the
   user should be told plainly not to enter a real roll log into it.
2. **Word count**: 12 (default) and 24.
3. **Separator**: none (default) and dash, labelled with the devices each matches.
4. **Roll entry**: a text area accepting digits, tolerant of spaces and newlines.
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

**Vector 4: Coldcard's own published example, 24 words.** Rolls `123456`, separator none. This
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

**Vector 6: the Krux dialect.** Same physical rolls, dash separator. Proves the separator is
actually reaching the preimage.

```
preimage  1-2-3-4-5-6
sha256    b76c3b0194c3c3b0e31e358d76ea00414bdacb2024c976c8d7963d896017f851
```

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

Vectors 4 to 8 use roll strings far below the minimum, or exactly at it. Expose an internal
entry point for the tests that bypasses the minimum-length check, or make the minimum a
parameter with a default. Do not weaken the check that the UI uses.

**One item to verify during implementation, not yet confirmed at the source:** the 12-word
truncation rule was
confirmed by reading Ian Coleman's `index.js` (`bits.substring(0, 32 * length / 3)`) and from
SeedSigner's documentation ("truncated to 16 bytes for 12 words"). Coldcard's `rolls12.py` was
not read directly. Before relying on vector 5, download `rolls12.py` from Coldcard's
verification page and confirm it prints `... payment owner` for input `123456`, or confirm the
same on the Mk4 itself. If it disagrees, Coldcard's behaviour wins and this plan is wrong.

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

## 10. Deployment to Tails

Mirror what `slip39-backup` already does, since it is known to work:

```bash
cd DiceToSeed.Web
dotnet publish -c Release -o publish
# copy publish/wwwroot to the USB stick
```

On Tails, with networking off:

```bash
cd /media/amnesia/<USB>/dice-to-seed
python3 -m http.server 9876 --bind 127.0.0.1
```

Then open `http://127.0.0.1:9876` in LibreWolf. A local web server is required because
WebAssembly will not load over `file://`. Confirm with `--bind 127.0.0.1` that nothing is
listening on an external interface.

Ship a `start-server.sh` alongside the published output, and document the two commands in the
README.

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
2. Add `english.txt` as an embedded resource. Write the integrity test first (2048 words, and
   the SHA-256 in section 6), then make it pass.
3. Implement `Bip39.cs`, entropy bytes to words. Drive it with vectors 1 to 3. This is the part
   most likely to have an off-by-one in the 11-bit split, so write those three tests before any
   implementation.
4. Implement `DiceRolls.cs`: whitespace stripping, character validation, minimum-length rules,
   all returning `Result`. Test the three failure modes and their messages.
5. Implement `DiceSeed.Derive`, the five steps of section 4. Drive it with vectors 4 to 8.
   Vector 5 is the one that catches a wrong truncation, so make sure it is present and passing.
6. Download Coldcard's `rolls12.py` and confirm vector 5 against it, per the flag in section 8.
   If it disagrees, stop and report before changing anything.
7. Build the single Blazor page against the Core API. Start with the roll counter and the live
   preimage, because those are the elements that carry the verification value.
8. Add the offline warning and the served-from-elsewhere escalation.
9. Add the verification panel with the section 9 content.
10. Grep the published output for external references (`http://`, `https://`, `cdn`, font
    URLs) and confirm there are none. Load the published app with networking disabled and
    confirm it works.
11. Write the README: what it is, the three-way verification procedure, the Tails commands, and
    an explicit statement that this app is a checker rather than a recommended primary
    generator.
12. Add the CI workflow.
13. Open a pull request. Do not merge it.

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

---

*Collaboration by Claude*
