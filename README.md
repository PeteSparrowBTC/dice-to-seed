# dice to seed

Turn a log of dice rolls into a BIP-39 seed phrase, offline, on a computer that has no way to
remember it.

### [Try the demo →](https://petesparrowbtc.github.io/dice-to-seed/)

See what it does, in your browser, right now. **Do not make a real seed there.** A hosted page
is for looking at, not for holding money: the rolls you type are the seed itself, and you have
no way to confirm the page you were served is the one in this repository. For a real seed,
download the release and run it offline, as below.

## Why you would use this

Your hardware wallet made your seed. How do you know it used your dice?

You cannot tell by looking. A seed made from a broken random number generator looks exactly
like a good one, and in July 2026 Coldcard firmware was found to have skipped its hardware
generator entirely for five years. Coinkite's own guidance was that seeds made with at least
50 fair dice rolls were not at risk.

Dice are how you supply randomness you can account for. **This app is how you check that the
device used it.** You enter the same rolls here, and the words must match. If they do not,
something in that ceremony did not do what you thought it did.

It is a second opinion, not a place to make a seed.

## What you need

- Dice you trust, and paper
- A computer running [Tails](https://tails.net), with the network off
- A USB stick

Tails matters because it forgets. It runs from RAM, keeps nothing, and everything disappears
when you shut it down. Your seed ends up on paper and nowhere else.

## Doing it

**Before you boot Tails**, download the AppImage from the
[latest release](https://github.com/PeteSparrowBTC/dice-to-seed/releases), check it, and copy
it to the stick:

```bash
sha256sum -c SHA256SUMS
```

**On Tails, with the network off**, copy it off the stick and run it:

```bash
cp /media/amnesia/*/dice-to-seed-x86_64.AppImage ~/
cd ~ && chmod +x dice-to-seed-x86_64.AppImage
./dice-to-seed-x86_64.AppImage
```

Run it from a terminal the first time. Double-clicking often appears to do nothing, because the
Tails file manager will not launch programs that way.

Then:

1. **Roll one die at a time.** Fifty rolls for twelve words, ninety-nine for twenty-four. Write
   each result down as you go.
2. **Press the face you rolled.** The buttons record what you rolled; nothing here rolls for
   you. Keys 1 to 6 work too, and Backspace undoes the last roll.
3. **Watch the counter.** Miscounting is the most common mistake in the whole exercise, and a
   log one roll short still produces a perfectly convincing seed phrase.
4. **Write the words on paper.** There is no copy button, deliberately.

Full instructions, including how to check your result against other tools:
**[TAILS_INSTRUCTIONS.md](TAILS_INSTRUCTIONS.md)**.

## Two things people get wrong

**Never re-roll a log because it looks wrong.** Fifty 1s is exactly as likely as any other
fifty rolls, and the seed is exactly as strong. Throwing away logs for looking non-random means
your seed is drawn from a smaller set than your dice offered, which is the one thing that
genuinely weakens it.

**If you throw several dice at once, use dice you can tell apart** and read them in the same
order every time. Four identical dice thrown together lose a third of their entropy if you
cannot record which was which, and the roll counter will still tell you that you did everything
right.

## What it will never do

**It has no random number generator.** No "roll for me" button, no simulated die, nothing
random anywhere. Every value comes off your dice. This is enforced by a test that fails the
build if a source of randomness ever appears in the code.

It also does not save, export, copy or send anything, and it makes no network request of any
kind. No addresses, no account numbers, no wallet features. It converts your rolls, shows every
intermediate value so you can check each step, and stops.

## Why you can believe the words

The app agrees with tools written by other people, and the test suite proves it on every
change:

- **SeedSigner's** published examples, from their own verification documentation
- **Coldcard's** published example, confirmed by running their own script
- the **official BIP-39 test vectors**, the complete published set
- the wordlist itself, checked against its published fingerprint every time the app starts

If any of those ever stopped matching, the build fails.

## Related

Splitting a seed you already have across several backups:
[slip39-backup](https://github.com/PeteSparrowBTC/slip39-backup). A vendor-neutral guide to
making one in the first place:
[seed-generation](https://github.com/PeteSparrowBTC/seed-generation).

## For developers

[CLAUDE.md](CLAUDE.md) has the engineering rules, [docs/](docs/) the design notes and platform
findings, and [VERSIONING.md](VERSIONING.md) what a version number means here.

```bash
dotnet test     # the vectors
```

---

*Collaboration by Claude*
