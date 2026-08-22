# Changelog

Semantic versioning, with MAJOR reserved for a change that would produce different words for
the same rolls. See [VERSIONING.md](VERSIONING.md).

## 1.4.2

The same words from the same rolls as 1.4.1. The roll sheet as a PDF as well.

### A PDF as well, committed

- `roll-sheet.pdf`, generated from the HTML and committed rather than built in CI. It is a blank
  sheet, so there is nothing in it to leak, and a PDF is the one format everybody can open and print
  without wondering whether their browser will scale it. Both ship with every release and both are
  served by the app, so the sheet is reachable as a file or as markup you can read first.
- Generated at A4 with the sheet's own `@page` margins and **no header or footer**: a printed URL and
  date on a sheet that says destroy me is the opposite of what it says.
- Committing a derived artifact costs one thing, which is that it can go stale silently. So the HTML's
  SHA-256 is recorded beside the PDF, and a test compares it against the file as it stands: edit the
  sheet without regenerating and the suite fails, naming the command to run. Verified by editing the
  HTML and watching it go red. The regeneration command lives in the comment at the top of the sheet.
- Asserted to be one page, on A4, from the page tree and the media box in the file itself. One page is
  the point of the layout work: a grid split across a break is useless, and a second sheet holding
  nine empty boxes is somebody wondering what they missed.
- The staleness guard hashed the file's raw bytes at first, which made it a hash of the checkout as
  much as of the sheet: git writes CRLF into a Windows working tree and LF into a Linux one, so it
  passed locally and failed on the runner with two different digests. It now normalises line endings
  before hashing, which is what it should have done from the start, since line endings cannot change
  what prints. The documented command normalises too, or it would disagree with the test on one of the
  two platforms.
- `.gitattributes` marks `*.pdf` and `*.png` binary explicitly. Content detection would have handled
  it, but a blanket text rule in the sibling `tails-appimage` repository stripped a byte from a PNG
  earlier in this project's life, and the fix there was to name binaries rather than hope.

## 1.4.1

The same words from the same rolls as 1.4.0. The roll sheet is now a link rather than a file path in
a repository.

### Served by the app

- `roll-sheet.html` moved into the app's `wwwroot`, so it is published with everything else. That
  makes it a link on the demo, a file inside the AppImage, and one copy rather than two that can
  drift.
- The page links to it twice: from the dice pad, where somebody who has not started yet will see it,
  and from the comparison hint that appears once rolls are recorded. The href is relative, so it
  resolves against the base tag and one link works at `/` in the AppImage and `/dice-to-seed/` on
  Pages. A test asserts it is relative, because an absolute one would be an external reference in an
  app that has to load with the network disconnected.
- Still a release asset, covered by `SHA256SUMS`, for anyone who would rather download than browse.

### Links had no styling at all

- The app had never had a link, so `app.css` had no rule for one, and the first anchor rendered in
  the browser's default `rgb(0, 0, 238)` on a near-black page. Found by reading the computed style
  rather than by looking at the markup, which is the only way this kind of defect shows up.
- Links now draw from `--accent` and keep their underline, since colour alone is not a link.
  Measured: 11.5:1 against the page background, where the standard asks for 4.5:1.
- `MarkupStyleTests` gained a guard, in the file that exists for exactly this failure. Its existing
  check compares classes against the stylesheet, and an element selector is not a class, so it could
  not have caught this. **Third instance of one shape of bug**, after the loading ring and the error
  strip's reload link: markup whose styling was assumed rather than written.

## 1.4.0

The same words from the same rolls as 1.3.10. A printable roll sheet, and the reason the paper exists
at all, which turned out to be sharper than "keep a record".

### The one error nothing else here can catch

- Every check this app offers compares the roll log against another implementation of the conversion.
  **None of them can see a mis-press.** Press 4 where the die showed 5 and both tools agree perfectly,
  the counter still reads sixty, `sha256sum` matches, Coleman agrees, `rolls12.py` agrees, and the
  words are valid BIP-39 for a wallet the dice never made. An independent note of what the dice showed
  is the only defence, and that is the whole argument for paper.
- The app was in the worst position on this: `TAILS_INSTRUCTIONS.md` said "write each result down as
  you go" with no reason given, the bundle's instructions said to destroy the paper "if you wrote them
  down", and the page said nothing at all. So it asked for a second plaintext copy of the seed and
  never asked for the comparison that would justify it. **Now the comparison is a numbered step**,
  placed immediately before Derive, in all three.

### `printable/roll-sheet.html`

- One page, printed on an ordinary machine before booting, because an amnesic offline session is the
  wrong place to be arranging a printer. Plain HTML, no script and no external reference, so it opens
  in any browser and can be read end to end in a minute. Blank, so it carries nothing until somebody
  writes on it.
- **Rows of ten, numbered by the position of the first roll**, matching the app exactly, so the
  comparison is a row-by-row read rather than a hunt through sixty undifferentiated digits, which is
  the very mistake being looked for. `RollRowTests` reads the row labels out of the sheet and asserts
  they equal the arithmetic the page uses, and it fails if either drifts, because everyone already
  holding a printed copy cannot reprint it.
- **It says destroy twice, in the largest type on the page**, and says why: a filled-in sheet is the
  seed in plain text, it is the one artifact that survives the shutdown erasing everything else, and
  it sits beside another sheet that looks equally important and is meant to be kept forever. It also
  says plainly that it is not a backup, because somebody keeping it as one has an unprotected copy of
  the wallet.
- **Deliberately no place to write which wallet it is.** No name, no date, no label, no amount. A found
  sheet of digits is bad; one that also says whose it is and when is worse. The two purpose tick boxes
  are the exception, and they earn it: rolling for a seed and for a backup key means two sheets, and
  one log used for both makes the key derivable from the wallet it protects.
- Heavier rules after rolls 50 and 60, with a legend saying what they mark. They were originally at 50
  and 110, which is not a count anyone stops at, and unlabelled: an unexplained line on a form is a
  question rather than an aid.
- Measured at print width rather than eyeballed: 240mm of content against 275mm on A4 and 254mm on
  Letter, so it fits both with room.

### On screen

- The recorded log is now also shown in **rows of ten with the position of each row**, beside the exact
  preimage, which is untouched and still rendered character for character as rule 4 requires. Row
  numbers are CSS generated content on a data attribute, the same device the backup key groups use, so
  selecting the block copies digits and never the numbering.
- `DiceRollLog.RowsOfTen` holds the grouping, tested to reassemble into exactly the preimage at every
  length from 1 to 120, so what is being checked is provably what is being hashed.

### Shipped rather than repository-only

- `roll-sheet.html` is a release asset, covered by `SHA256SUMS` with the other files, and the release
  notes say what it is for. The workflow's inventory of what a release contains was two files out of
  date and still carried the "only thing a downloader can check it against" line that 1.3.3 corrected
  everywhere else.

## 1.3.10

The same words from the same rolls as 1.3.9. A review asked what was missing, and the answer was
the test behind the rule that protects the backup key.

### The switching rule moved out of the view and into Core

- Rule 9's first clause is "switching mode clears the roll log". It was implemented correctly and
  **tested nowhere**: the hazard it protects against is asserted by `BackupKeyTests`, while the
  protection itself was four lines inside a 1267-line Razor component that nothing would have
  noticed the removal of. For a property this load bearing that is the wrong arrangement.
- `DiceToSeed.Core/ModeSwitch.cs` now owns the decision, and `DerivationMode` moved with it.
  `Apply` returns the mode to be in and the log that survives the transition, which is nothing
  whenever the mode changed, so the page assigns both from one call and cannot end up in the other
  mode with a log still recorded.
- Twelve new tests, 172 total. The invariant is checked over every combination of modes and several
  logs rather than the interesting one: **no path through `Apply` changes mode and keeps rolls.**
  The other half is checked too, since it would make the app unusable if it broke: pressing the
  mode you are already in keeps the log, and the confirmation is raised only when there is really
  something to lose. An app that asks when nothing is recorded trains the user to dismiss the
  question, and then it gets dismissed the once it matters.

### A guard that matched its own documentation

- The test asserting the page routes through `ModeSwitch` first checked whether `Derive.razor`
  contained the string `ModeSwitch.Apply`. It passed with the call deleted, because the XML doc
  comment one line above says `see cref="ModeSwitch.Apply"`. It now matches the assignment,
  `(mode, rolls) = ModeSwitch.Apply(`, and was confirmed to fail with the clearing removed and pass
  with it restored.
- Third time this shape of mistake has appeared here, after the favicon's `xmlns` and the loading
  ring, so it is written down in the test: assert the thing that does the work, not the words
  describing it.

### Verified in a browser, since this touched live UI

Ten rolls recorded, then: pressing the mode already in effect kept all ten and raised no
confirmation; requesting the other mode raised the confirmation and changed nothing yet; cancelling
kept all ten; confirming landed in the other mode with the counter at zero.

## 1.3.9

The same words from the same rolls as 1.3.8. The salt water test, done properly: whether it will
work on your die is decided by which plastic it is, and 1.3.8 imported a d20 result into a d6 tool.

### Whether the die floats at all

- 1.3.8 said "many dice never float", from a comparison that got 4 of 22 to float. Those were
  **acrylic d20s**, and this is a d6 tool, so the result was quoted more broadly than it holds.
- Saturated brine is **1.20 g/cm³**, at about 26% salt by weight, which is 357 g per litre and more
  than most people expect. Against that:

  | | density | in brine |
  | --- | --- | --- |
  | cheap opaque board-game die, ABS or polystyrene | 1.02 to 1.05 | floats easily |
  | translucent acrylic die, the usual RPG dice | 1.19 | floats by 1%, mostly submerged, settles ambiguously |
  | casino precision die, cellulose acetate | heavier | sinks |

- So for a cheap d6, which is what this app is for, the test usually works. The 4-of-22 result is the
  acrylic row, which sits within one percent of neutral buoyancy, and that also explains why it gives
  mushy answers rather than clear ones: a nearly neutrally buoyant die has almost no righting torque.
- What it does not do is unchanged: it tells you where the mass sits, not which face lands upward.

### Why counting your own rolls cannot substitute

Elementary arithmetic rather than a power table, so it can be checked with a calculator, and now
computed in `RollEntropy` rather than asserted.

- Over 60 rolls a face at one in five is expected **12 times against a fair 10**. The standard
  deviation of that count is **2.9**, so the excess of 2 is **0.7 deviations**: smaller than the noise
  it has to be seen against.
- Three deviations clear takes **1,125 rolls**. For the bias real dice actually have, **262,223**.
- **Weldon's dataset is 315,672 rolls**, and that is not a coincidence: it is the same calculation from
  the other end. The only solid measurement of ordinary dice bias comes from a Victorian throwing dice
  a quarter of a million times because a quarter of a million rolls is what the measurement costs.
  That single fact is the argument for inspecting the die instead, and it arrived from an assertion of
  mine that was wrong by a factor of four.

### Under it

- Three new tests, 160 total: the 0.7-deviation signal at 60 rolls, the 1,125-roll figure with a round
  trip so the inversion cannot be quietly off by one, and the 262,223 figure checked against Weldon's
  count.

## 1.3.8

The same words from the same rolls as 1.3.7. How to actually throw the die, which the app had said
nothing about, and a test it had oversold.

### How to throw it

A new section on the page, in `TAILS_INSTRUCTIONS.md`, in the README and in the bundle's
`READ-THIS-FIRST.txt`. This is the one part of the ceremony the app cannot do, and it had no
coverage at all.

- **A hard flat surface, and make the die hit something.** A table rather than carpet, which absorbs
  the bounce and lets a die settle without turning over. The load-bearing reference is casino craps,
  where both dice must strike the far wall and that wall is lined with rubber pyramids. That rule
  exists to destroy whatever the thrower set up, which is exactly the property wanted here, and it is
  a published convention rather than a recollection.
- **The test is tumbling, not a height.** Several turns and at least one bounce. Sliding, spinning it
  flat, or dropping it from an inch can carry the starting face straight through. No height in
  centimetres is given, because the observable is the tumbling and a number would be invented
  precision. The reliable method is an opaque cup: shake and tip out.
- **The off-the-table rule is fixed before the first throw**, and this is the part that matters. If
  the die leaves the surface, lands leaning rather than flat, or cannot be read cleanly, that throw
  does not count. The danger is not the re-throw: a die on the floor is unconnected to the number it
  would have shown, so discarding it costs nothing. Choosing to discard a roll *after* reading a
  number you did not like is a different act, and it narrows the set your seed came from in the same
  way re-rolling a whole log does. Apply the rule before reading the face, and never discard a roll
  already recorded.
- **Press the button after each throw**, so the counter and the throws cannot drift apart.

### The float test was oversold

- It was described as "the test that works... finds a loaded die in a minute". Two corrections. The
  practical one first: **many dice never float**, whatever you do, because saturated brine is about
  the density of the plastic. One published comparison got 4 of 22 dice to float, so "keep adding
  salt" can be a dead end, and sending someone off to keep going until it works is worse than telling
  them it might not.
- It also measures where the centre of mass sits rather than which face lands upward, which is related
  but not the same. The same comparison found its predictions unsupported by rolling; that part is
  weak evidence in itself, since 100 rolls of a d20 is five expected per face and has almost no power,
  so it is reported as what it is rather than adopted as a conclusion.
- It stays in the text as a way to catch a weight glued into a novelty die, and no longer as a
  fairness measurement.

### No video

- A video was asked for and none is linked. The app cannot carry one at all: rule 6 forbids external
  references, CI fails the build on one, and it runs offline where a link is useless. In the README a
  link would be possible, and the honest obstacle is that video content cannot be verified from here,
  while "dice control" tutorials teach the opposite of what this section recommends. Linking one
  unwatched, in guidance for key generation, is the kind of unverified claim this repository exists
  not to make. The [dice control](https://en.wikipedia.org/wiki/Dice_control) reference is linked
  instead, since it explains the back-wall mechanism and can be read rather than trusted.

## 1.3.7

The same words from the same rolls as 1.3.6. **One die, thrown repeatedly**, is now the
recommendation everywhere, and the advice it replaced was mitigating the wrong risk.

### The ordering trap, with its arithmetic

- 1.3.2 added "rotate several dice if you have them, so a bad one touches only its share of the log".
  That hedged manufacturing bias, which the same page measures at about **one bit in a hundred and
  fifty-five**, by introducing an ordering question worth **a third of the log**. The mitigation cost
  more than the thing it mitigated, which is exactly the trade this app exists to catch. It is gone.
- **Five identical dice landed in a heap hold 12.9 bits only if you can say which die is which.** If
  you cannot, what you recorded is the set of faces rather than a sequence, and there are 252 of
  those: 8.0 bits, a 38% loss. A 60-roll log thrown that way carries **95.7 bits against a target of
  128**, while the counter reads 60 and the words look exactly as convincing. Nothing downstream can
  detect it, which is why it is a warning rather than a check.
- Throwing a handful at once stays legitimate, and the conditions are stated: dice you can tell apart,
  a fixed reading order every time, and never sorted into ascending order, which is the same mistake
  performed on purpose.
- The README carried "four identical dice lose about a third of their entropy" with no number behind
  it. The figure was right; it now comes from `RollEntropy` and a test holds it at 32 to 34%.

### Where it says so

- The headline in "Before you start" is now "Roll one die 60 times for twelve words", carrying the
  recommendation at the top level for the cost of one word rather than a fourth hint above the pad.
- `READ-THIS-FIRST.txt` in the Tails bundle explains it without arithmetic, since its reader is
  someone who does not use a terminal: one die means the order is the order you threw them in.

### Under it

- Seven new tests, 157 total. They pin the multiset counts against values small enough to check by
  hand (one die 6, two dice 21, four 126, five 252), the 38% and 33% losses, the 95.7-bit consequence,
  and that a single die loses exactly zero, which is the sanity check that the advice and the
  arithmetic agree.

## 1.3.6

The same words from the same rolls as 1.3.5. Three things 1.3.5 got wrong or left out, two of them
found by running it on Tails rather than reasoning about it.

### The zip had no published hash

- `SHA256SUMS` covered the AppImage only, so the artifact whose entire purpose is verification was
  itself unverifiable: anyone wanting to check the zip before extracting had nothing to check
  against. It now covers both files, which meant building the bundle before the checksums rather
  than after.
- Since a downloader usually takes one and not the other, the docs give
  `sha256sum -c --ignore-missing SHA256SUMS`. Without that flag the file you deliberately did not
  download is reported as a failure.

### "Double-click start-here.sh" was wrong

- GNOME Files opens an executable `.sh` in the **text editor** on a double-click. The instructions
  now say to right-click and choose **Run as a Program**, which is what actually works and what was
  confirmed on a real session. Properties and "Executable as Program" stays as the fallback for when
  that entry is missing from the menu.

### The AppImage in the zip is deliberately not executable

- Confirmed on Tails: Archive Manager restores the modes a zip stores, so 1.3.5's AppImage arrived
  ready to double-click and ran with no chmod step at all. That reads as a convenience and is a hole.
  Double-clicking the app was easier than right-clicking the checker, so the fastest route into the
  application was the one that skipped the verification the bundle exists to provide.
- It is now stored 644, and `start-here.sh` sets the bit itself after the hash matches. Bypassing
  the check still works through Properties, which is a deliberate act rather than an accident, and
  `READ-THIS-FIRST.txt` says the app is switched off until it has been checked so that a click on it
  doing nothing reads as intended rather than broken.
- `build-bundle.sh` asserts **both** modes, the launcher executable and the AppImage not, and fails
  the build if either is wrong. Asserting only one would have missed the defect that matters more.

### Field notes

- `docs/tails-platform-notes.md` had "GNOME Files does not launch binaries on double-click" at the
  top of its troubleshooting list. **That is wrong for Tails 7** and is corrected, with the date and
  the Nautilus version: an AppImage carrying its bit launches on a double-click. The claim was
  sending people to a terminal to diagnose a problem they did not have. Scripts are the genuine
  exception and now have their own entry.
- The manifest lookup for the GUI tools is recorded: `zenity` 4.1.90, `file-roller` 44.5, `7zip`
  25.01, `nautilus` 48.3, `gnome-console` 48.0.1, and **no `unzip` or `zip` at all**, so any
  instruction containing the word `unzip` is wrong for this platform.

## 1.3.5

The same words from the same rolls as 1.3.4. A second release artifact, for the person who is
handed a USB stick rather than the person who reads this file.

### `dice-to-seed-<version>-tails.zip`

- The instruction to check the SHA-256 was one nobody could follow where it mattered. At an offline
  Tails machine there is no release page to read and no documentation on the stick, so the check
  existed for whoever had a terminal and a second screen. The zip carries the AppImage, its
  `SHA256SUMS`, `READ-THIS-FIRST.txt` written for someone who does not use a terminal, and
  `start-here.sh`, which checks the app against its fingerprint and **refuses to open it** if they
  disagree.
- Extract in the Files window, copy the folder into Home, double-click `start-here.sh`. No terminal
  at any point. If double-clicking does nothing, Properties and "Executable as Program" fixes it,
  which is the same single step the bare AppImage already needed.
- **Results go through `zenity`, not stdout.** A script launched from a file manager has no terminal,
  so a failure printed to stderr is a failure nobody sees, which is worse than no check because it
  teaches the user that silence means success. Tails ships zenity 4.1.90, confirmed in the 7.10.1
  package manifest rather than assumed, and the script falls back to stderr where it is absent.
- The checker launches the app on success, deliberately. Telling someone to verify by hand and then
  launch by hand makes verification the step that gets skipped; this way the only route to the app
  is the one that checks it first.
- **What it does not claim.** `SHA256SUMS` travels in the same folder as the app it describes, so
  whoever could replace one could replace all three. The check proves the file was not damaged,
  half-copied or altered on the stick; it cannot prove the download was genuine, and whoever
  prepared the stick is who is being trusted for that. Both the script's own dialog and
  `READ-THIS-FIRST.txt` say so in plain words.
- The loose AppImage and `SHA256SUMS` still ship, for verifying by hand or scripting against, and
  the release notes now say which of the two to take.

### The bundle is built and tested on every pull request

- `packaging/tails-bundle/build-bundle.sh` is called by both `ci.yml` and `release.yml`, so the
  artifact a pull request exercises and the artifact a release publishes cannot drift. Assembling it
  only on a tag would mean its verification step was first exercised by whoever downloaded it.
- It asserts that the zip **stores** the executable bit, since that is a property of the zipping
  tool rather than a certainty, and it feeds the checker a corrupted AppImage and a missing
  fingerprint and fails the build if either is accepted. A verification that cannot fail is
  decoration.
- What it deliberately does not assert: that the extractor on Tails **restores** that bit. Tails has
  no `unzip`, extraction goes through Archive Manager or 7zip, and the answer is unverified, so the
  instructions tell the user how to set the bit and nothing depends on the archive carrying it.

## 1.3.4

The same words from the same rolls as 1.3.3. The demo banner is shorter.

- 1.3.3 corrected the banner's claim that the hosted copy was a way to read the code, and then
  explained itself: two sentences about WebAssembly and where the source lives. **That explanation is
  gone.** The banner's one job is to stop a real roll log being typed into a web page, and a warning
  about losing a wallet should not share space with an aside about how the app is compiled. It now
  says what the page is for, then the hazard, then what to do instead.
- The reasoning stays in the `pages.yml` comment, where the person editing the banner reads it, with
  a note not to put it back.

## 1.3.3

The same words from the same rolls as 1.3.2. Two claims corrected, both of them overclaims in the
same direction: a check described as stronger than it is.

### The demo is for trying the app, not for reading it

- The banner said "a hosted copy, for trying the app and reading the code". You cannot read the code
  there. The derivation compiles to WebAssembly, so the page shows what the app does and not how it
  does it, and reading it means reading the source. This is the same overclaim that got
  `dice-to-seed-wwwroot.zip` dropped in 1.2.0, made again about the page that replaced it.
- The workflow comment that described the deployment as existing "to demonstrate and to be read" is
  corrected in the same terms.

### What the checksum proves, and what it does not

- **Tails does not make the checksum redundant**, and the docs now say why rather than leaving it to
  be assumed. The two protect different things: Tails decides whether your seed can get out, while
  the checksum decides whether the program deriving it is the one published here. A tampered build
  of this app needs no network at all. It only needs to show you twelve words its author can also
  compute, and an offline amnesic session runs that faithfully and forgets it perfectly.
- Said plainly instead of implied: the check catches a corrupted or truncated download and a stick
  altered afterwards. It is **not a signature**, and `SHA256SUMS` travels with the file it
  describes, so anyone able to substitute one can substitute the other. For that, compare against
  the hash printed in the tagged run's public build log, which is a separate thing to have to
  compromise.
- `VERSIONING.md` said the checksum was "all a downloader can check it against", which was the same
  overclaim in the place that documents the release. Corrected there and in the release notes
  template.

## 1.3.2

The same words from the same rolls as 1.3.1. One visible defect, present in every release so far,
and the check that would have caught it.

### The loading screen was a black lump

- `index.html` carried the Blazor template's loading indicator, two SVG circles with class
  `loading-progress`, while this app **replaced** the template's stylesheet rather than editing it.
  The rules that size and stroke those circles live in the template's stylesheet, so the markup
  arrived with none. SVG fills black by default and an `<svg>` with no width collapses to its
  parent, so what appeared while the runtime downloaded was a dark blob with an empty caption under
  it. It shipped in 1.0.0 through 1.3.1, on the demo and in the AppImage.
- Replaced rather than restored: a determinate bar, the app's name, and one line saying the app
  makes no network call. Fewer rules than reinstating the ring, and it is markup this repository
  owns instead of template leftovers.
- **The percentage is real.** The runtime sets `--blazor-load-percentage` and
  `--blazor-load-percentage-text` on the document element as each file lands, so the bar reports
  progress rather than animating regardless, and a load that stalls looks stalled. That matters more
  than it sounds: a blank or spinning window in a tool that derives keys invites a reload halfway
  through.
- The reload link in the error strip had the same omission and no rule of its own, so it took the
  browser's default link colour on a red background. It only ever showed when something had already
  gone wrong.

### A test for the class of defect, not the instance

- `MarkupStyleTests` asserts that **every class `index.html` uses has at least one rule in
  `app.css`**. That is the exact shape of this bug, which no compiler can see because markup and
  stylesheets are copied rather than checked against each other. It found the reload link as well as
  the ring.
- It carries the usual two companions: a proof that the matcher would have rejected the shipped
  markup, and a check that the scan reaches the files at all rather than passing on an empty set.
  Confirmed to fail, naming the offending class, by removing one rule.
- 150 tests.

## 1.3.1

The same words from the same rolls as 1.3.0. One claim on the page had no number behind it, and
this release puts one there.

### "Worse than real dice are" is now a measurement

- The entropy table's conservative column was dismissed with the words **"which is worse than real
  dice are"**, and that was the whole argument. The roll counter said the same thing at the
  recommended count. It is an assurance rather than a statement, which is what this app exists not
  to make.
- The number is Weldon's, 1894: 315,672 rolls of ordinary pipped dice, with a 5 or a 6 appearing
  **33.77% of the time against an expected 33.33%**. It is the largest published count on ordinary
  dice. The mechanism is understood and is not a defect: the spots are drilled out and filled with
  lighter paint, so the six face is missing the most mass and lands upward slightly more often,
  which is why casino dice have flush pips of matched density.
- Across a 60-roll log that die costs **0.004 bits** on the average measure and **1.1 bits** on the
  min-entropy floor, out of 155.1 collected against a target of 128. The two figures differ by a
  factor of about 300, so the page quotes both: citing one alone is how this subject gets
  misrepresented in either direction.
- The pessimistic column's die, one face at a fifth, is **15 times** more lopsided than that
  measurement. That factor replaced the assurance, in the table's legend and in the counter.
- **The table gains a "real dice" column** between fair and floor, because the comparison is the
  argument. Two things become visible in it: a real die still clears 128 bits at the 50-roll
  minimum, at 128.3, and the 99-roll shortfall sits in every column, so it is the roll count and
  not the dice.
- The README had applied the pessimistic die's half-a-bit Shannon figure to ordinary dice, which
  is wrong by three orders of magnitude. Corrected, with the same table.

### When to suspect a die, since fairness is the wrong question

- Working those numbers moved the advice. The risk worth managing is not statistical: a die goes
  badly wrong because someone made it that way or because it is damaged, and no statistic computed
  from a log will say which die produced it. So the page now covers the physical object: the
  reasons to set a die aside, the saturated-salt float test that finds a loaded one in a minute,
  and the two things that matter more than the die, which are throwing it so it tumbles and
  rotating several dice if you have them.
- The old line, "test a die beforehand if you want to", left the reader to guess whether it
  mattered. It does not, and the page says so with the arithmetic behind it.
- Why not to test the rolls you are about to use is stated with its power rather than asserted: at
  60 rolls a chi-squared test misses a real 20% bias most of the time while failing one honest log
  in twenty, and re-rolling on failure conditions the seed on passing a test, which shrinks the
  output space.
- It also states the limit. A roll log carries no evidence of the die that produced it, so none of
  this is checkable afterwards by anyone, which is why it is words on a page and not a feature.

### Fixes

- `TAILS_INSTRUCTIONS.md` still described the missing copy button as a safeguard the app maintains
  because it "cannot tell the difference" between machines. That claim was retired from the rules
  in 1.3.0 and the instructions kept it. Any text on the page can be selected and copied; what
  holds is that the app never writes to the clipboard itself.

### Under it

- Eleven new tests, 145 total. They pin the distribution, the one-bit claim, the factor of fifteen,
  the ordering of the three models at every roll count the table shows, and that a real die clears
  its target at 50, 60 and 111 rolls. Every figure the page quotes is computed in `RollEntropy`,
  because a number typed into a UI string rots silently.

## 1.3.0

The same words from the same rolls as 1.2.0. This release is what came out of running the AppImage
on Tails and reading the page as a user rather than as its author.

### The page is ordered for someone using it

- **"Before you start" is above the dice pad**, where advice about what to do first belongs. It was
  at the bottom, past the point where anyone had already started rolling. It is collapsible and open
  on load, because expanded it pushes the dice below the fold in the 1000x900 window the AppImage
  opens at: the pad sits at y=852 open and y=657 collapsed. Read it, close it, roll.
- The entropy table moved behind its own disclosure. Fifty lines of arithmetic is not what "before
  you start" means.

### The word "preimage" is gone from the interface

- The result said "Preimage (the rolls, hashed as-is)", which claimed the value shown was already a
  digest and used jargon to do it. The labels now read **What will be hashed**, **What was hashed:
  your rolls, and nothing else**, and **SHA-256 of that string**. For d6 the value is the roll log
  character for character, and the label says so instead of naming the concept.

### The backup key can be copied, and says where it goes

- **The key was renderable only as sixteen numbered groups**, and selecting that block dragged the
  group numbers into the clipboard, so a paste produced `1 6cb0 2 9af8 3 5505` rather than the key.
  The unbroken 64-character line is now the primary rendering, with the grouped view kept below it
  and labelled for pen and paper.
- The group numbers became CSS generated content on a data attribute. `user-select: none` was tried
  first and is not enough: it prevents a mouse selecting the number while leaving the text in the
  DOM, so a copy still reaches it. Generated content cannot be copied by any route.
- The key and the check code are now boxed together under **"This is what goes into
  slip39-backup"**, followed by "Those two values, and nothing else on this page". They used to be
  three step labels apart with commentary in between, so which values to transfer was a fair
  question.

### The banner decision is tested

- The Pages workflow described the red warning on a hosted build as "the only thing standing between
  a curious visitor and a real roll log typed into a web page", and said the behaviour was tested.
  Nothing tested it: it was an inline expression comparing the host against three strings. It now
  lives in `DiceToSeed.Core/ServingOrigin.cs` with 25 cases, including the hosts a shortcut waves
  through, such as `127.0.0.1.example.com` and `localhost.evil.com`.
- The check also got wider where it was too narrow, and one case was a latent defect: a non-web
  scheme is local, because `tauri:` and `file:` are an in-process handler rather than a network; the
  whole loopback range counts, not only `127.0.0.1`; and anything under `.localhost` counts, per RFC
  6761. Tauri serves from `tauri.localhost` on Windows, so a Windows desktop build would have warned
  against itself on every launch.

### Packaging

- **The AppImage filename carries the version**: `dice-to-seed-1.3.0-x86_64.AppImage`. The AppImage
  is the file that survives on somebody's USB stick long after `SHA256SUMS` has been deleted, and a
  bare name tells its owner nothing about which release they hold. Anything scripted against
  `dice-to-seed-x86_64.AppImage` needs updating; the release notes and docs glob on the pattern.

### Honesty about the clipboard

- Rule 6 listed "no clipboard writes of seed material" beside "no network call", which reads as a
  safeguard. It is not one: any text on the page can be selected and copied. What is true, and what
  the rule and the footer now say, is that the app never writes to the clipboard itself, so nothing
  lands there unless you put it there. Copy buttons were considered and not added.

## 1.2.0

The same words from the same rolls as 1.1.0. This release is about the page telling the truth and
being usable by someone who is not comfortable in a terminal.

### The vendor minimums are not sufficiency proofs

- A fair d6 carries log2(6) = 2.585 bits, so **99 rolls carry 255.9 bits against a target of 256**.
  The 24-word minimum does not reach its target even with a perfect die, and the page said nothing
  about it. It does now, with a table.
- Recommends **60 rolls for 12 words and 111 for 24** when making a new seed, chosen so the
  conservative min-entropy floor clears the target rather than the average just about reaching it.
  The counter targets those numbers, and a result derived below them carries a warning beside the
  words.
- The minimum stays at the vendor numbers of 50 and 99. Coinkite's advice after the 2026 firmware
  defect was that seeds of at least 50 rolls were unaffected, so the people with the strongest
  reason to use this app hold 50-roll seeds and must be able to reproduce them.

### The runbook was unusable in two ways

- **Commands referred to `$ROLLS` and never set it.** An unset variable expands to nothing, so
  anyone following the runbook hashed the empty string and got `e3b0c442...b855` every time: a
  plausible hash matching nothing, whose obvious reading is that the app is wrong. Every command
  now carries your actual rolls, so a line goes straight into a terminal.
- **The hardest cross-check came first.** Now ordered easiest first: Coleman's page needing no
  terminal, then Coldcard's `rolls12.py` as one command, then Trezor's reference library with
  numbered preparation steps.
- The hash commands are labelled as checking step 2 rather than the words, because `sha256sum`
  knows nothing about BIP-39.
- Coleman's page has an address now, and the Dice-versus-Base-10 trap is something you can watch
  happen on his Filtered Entropy line rather than take on trust.

### Backup key mode

- The output is `k`, and `k` is the dice and nothing else. Mixing in a generated value by XOR was
  considered and rejected: it would hedge a biased die at the cost of making `k` impossible for
  anyone to recompute, so nobody could confirm the tool used the dice they rolled.
- States its limit: `k` wraps the backup, while the file key inside the age format is generated by
  the consuming tool and is what the payload is encrypted under.

### The page says which build it is

- Footer shows the version and the commit, so a build can be checked against a release tag.

### Tails, not a suggestion

- The banner says to use Tails and nowhere else, with the reason. The claim that there is no copy
  button because the app cannot tell where it is running is gone: it was never a protection, since
  any text on the page can be selected and copied.

### The release is one file

- `dice-to-seed-wwwroot.zip` and the local-server browser route are dropped. The AppImage runs on
  Tails from the file manager, so the second route was a second set of instructions with its own
  port to check and its own Tor Browser proxy caveat. The zip was also described as the artifact "a
  person can read", which oversold it: 142 of its 155 files were the compiled runtime.
- **The demo now deploys on a version tag**, the same event that publishes the release, so the two
  are always the same version.

### Fixes

- The favicon was still the Blazor template's purple logo. It is now the die that the AppImage
  uses, as an SVG.
- The external-reference check in all three workflows failed on `favicon.svg`, because an SVG must
  declare the `http://www.w3.org/2000/svg` namespace and the check could not tell an XML identifier
  from an address. Namespace values are now stripped before the check, and a namespace sharing a
  line with a genuine external URL is still caught.

## 1.1.0

A second mode, for the key that encrypts your backup. The seed conversion is untouched: the same
rolls produce the same words as 1.0.0.

### The backup key mode

- Rolls for `k`, the 32-byte key [slip39-backup](https://github.com/PeteSparrowBTC/slip39-backup)
  encrypts with and splits into shares. That key otherwise comes from a generator nobody can
  check, which is the thing this app avoids everywhere else.
- `k = SHA-256(the bare digit string)`, all 32 bytes. No new convention: it is the value the seed
  mode already shows at step 2, and `printf '%s' "$ROLLS" | sha256sum` reproduces it.
- **Renders hex and never words.** That is what makes a mode selector safe to offer here. A mode
  whose wrong position still produces plausible output is Ian Coleman's "Dice versus Base 10"
  trap: a different wallet, no warning. Hex and words differ in kind, so a mis-set mode shows.
- **Switching mode clears the roll log**, with a confirmation that gives the reason. One log must
  never yield both: on 24 words the BIP-39 entropy is that hash byte for byte, so a reused log
  makes `k` identical to the wallet it protects and the shares stop protecting anything.
- Shows a four character check code, because `k` is transcribed by hand and, unlike words and
  shares, carries no checksum of its own: any string is a valid passphrase, so a mistyped key
  encrypts cleanly and is discovered at recovery. The code is the first four characters of the
  hex SHA-256 of the printed hex key, computed over the string on screen so a shell reproduces it
  without decoding.
- Optional, and the documentation says what it does not buy: AgeSharp fills the age file key from
  its own generator and encrypts the payload under that, and `k` only wraps it. Dice give `k` a
  provenance you can account for, which is a smaller claim than removing every generator. Rolling
  for the seed remains mandatory, because entropy quality is the one property no later step can
  check.

### Fixes

- `TAILS_INSTRUCTIONS.md` offered Ian Coleman's page as a cross-check without saying to set the
  entropy type to Base 10 rather than Dice. The Dice type rewrites every 6 to a 0 before hashing,
  so a reader following the old text had a coin flip between the right answer and a confident
  wrong one. It now also says to test the setting with a log containing a 6, since the two types
  agree on every log without one.

### Under it

- The roll minimum is now stated against an entropy target rather than a word count, which is
  what always determined it. A test asserts both paths give the same number.
- Ten new tests, including one that pins the reuse hazard rather than a feature: it asserts that
  the same log makes `k` identical to the 24-word entropy, so if the derivation ever moves, the
  warnings built on it get revisited.

## 1.0.0

First release.

### The app

- Converts a log of six-sided dice rolls into a BIP-39 seed phrase, 12 or 24 words, using the
  convention Coldcard, SeedSigner and Krux all share for d6: the bare digit string, hashed with
  SHA-256, truncated rather than re-hashed, checksum taken from the truncation.
- Rolls are recorded with six dice-face buttons rather than typed, so nothing but a die face
  can enter the log. Keys 1 to 6 and Backspace do the same.
- Shows every intermediate value, and renders the exact preimage it hashes so it can be
  compared character for character against another tool.
- Vendor minimums, unmodified: 50 rolls for 12 words, 99 for 24.

### Verification

- The complete published BIP-39 English vector set, as upstream's file byte for byte, with its
  SHA-256 asserted so it cannot be edited into agreement.
- Coldcard's published dice example, confirmed by downloading and running Coldcard's own
  `rolls12.py`.
- SeedSigner's published 50-roll and 99-roll examples.
- The wordlist verified at startup against its published SHA-256; the app refuses to derive
  anything if it does not match.

### What it will never do

- No random number generator of any kind, enforced by a test that scans first-party source and
  fails the build. No "roll for me", no simulated die, no nonce, no id.
- No storage, no network call, no telemetry, no clipboard writes, no BIP-32, no addresses.

### Distribution

- An 11 MB AppImage for Tails, built with Tauri and packaged without bundling WebKitGTK, which
  Tails already ships. Verified against the Tails 7.10.1 package manifest rather than assumed.
- The static site, published alongside it, as the artifact to read.
- A demonstration build on GitHub Pages carrying a banner that cannot be dismissed.

### Notes for the curious

Two findings from building this are recorded in the repository because they change what a
careful person does:

- There is **no d6 dialect split**. Krux joins d6 rolls with nothing, exactly as Coldcard and
  SeedSigner do, and reserves the dash for d20 where a face value can be two digits. An earlier
  plan for this app was wrong about that, and a separator control was removed rather than
  shipped.
- **A roll log of all 1s cannot detect the most dangerous misconfiguration** in Ian Coleman's
  page, because its "Dice" entropy type differs from "Base 10" only where a 6 appears. The
  easiest log to type is the one that proves least.
