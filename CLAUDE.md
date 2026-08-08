# Working in this repository

This repository is a single offline Blazor WebAssembly app that turns a log of six-sided
dice rolls into a BIP-39 seed phrase. Its whole reason to exist is to be a **second,
independent implementation** that a Coldcard, a SeedSigner or Ian Coleman's tool can be
checked against. That purpose sets the engineering rules: the conversion must be small
enough to read in one sitting, it must carry published test vectors, and it must never
acquire a dependency whose behaviour cannot be verified from the repository itself.

The app derives keys that will hold real money. Treat every change as a change to
key-generation code.

The app converts entropy the user brought on physical dice. It never produces entropy of
its own, and it is built to be run from a USB stick on an offline Tails session. Rules 1
and 8 below are the two that carry those properties, and neither is negotiable.

## CRITICAL: main moves only through a pull request

- **Never push to main.** Not `git push origin main`, not a bare `git push` while main
  is checked out, not `git push origin HEAD:main`, and no `--force` variant. Open a pull
  request instead.
- **Never merge a pull request.** Not `gh pr merge`, not the REST API, not the web UI.
  Opening the PR is the agent's job; merging is the human's.
- Pushing feature branches (`git push -u origin <branch>`) is safe and expected.

### The three mechanisms, and what each actually does

| mechanism | what it actually does |
| --- | --- |
| GitHub branch protection on `main` | **The real enforcement.** Server-side, survives a reclone, applies to every client and to the web UI. Requires setup once per repo (below). |
| `.githooks/pre-push` | **Blocks locally**, so a mistake fails before the network round-trip and prints the way out. Opt in per clone: `git config core.hooksPath .githooks`. Bypassable with `--no-verify`, by design. |
| `.claude/settings.json` deny rules | Stops an agent from issuing the common main-targeting push and merge commands, plus the `gh api` verbs that could remove the protection itself. Matching is prefix-based and cannot cover every spelling, so it is a backstop for judgement, not a replacement. |

Deliberately **not** used: an `on: push` workflow that "prevents" direct pushes. Such a
workflow runs after the server has already accepted the push, so it can only report, never
block. A permanently red check is worse than no check.

### Setup: enable branch protection (once per repo)

Requires admin on the repo, and a public repository for the free plan.

```bash
gh api -X PUT repos/PeteSparrowBTC/dice-to-seed/branches/main/protection --input - <<'JSON'
{
  "required_status_checks": null,
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "required_approving_review_count": 0,
    "dismiss_stale_reviews": false,
    "require_code_owner_reviews": false
  },
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false
}
JSON
```

`required_approving_review_count` is 0 because GitHub does not allow self-approval, so any
value above 0 makes every pull request unmergeable until a second maintainer exists. A pull
request is still required. Raise it when there is one.

Verify:

```bash
gh api repos/PeteSparrowBTC/dice-to-seed/branches/main/protection \
  --jq '{pr_required: (.required_pull_request_reviews != null), admins_included: .enforce_admins.enabled}'
```

### Enable the local hook after cloning

```bash
git config core.hooksPath .githooks
```

## Engineering rules specific to this repository

These are not style preferences. Each one exists because this code generates private keys.

1. **The app is never a source of entropy, and a test enforces it.** There is no "roll for
   me" button, no simulated die, no "fill with random rolls" developer convenience, and no
   random value anywhere in the derivation path. The name invites the feature; the feature
   turns an audit tool into a browser-based key generator, which is the worst available
   place to make a key. Physical dice, rolled by the user, are the only accepted input.

   The rule is enforced by a test in `DiceToSeed.Tests` that reads the first-party source
   files and fails on any occurrence of `RandomNumberGenerator`, `System.Random`, `new
   Random`, `Guid.NewGuid`, `crypto.getRandomValues`, `Math.random` or
   `GetNonZeroBytes`. Two scoping details matter, and getting either wrong produces a test
   that is useless or permanently red:

   - Scan **first-party source only**: the `.cs`, `.razor`, `.js` and `.css` files under
     `DiceToSeed.Core` and `DiceToSeed.Web`, with `bin`, `obj` and `wwwroot/_framework`
     excluded. Do **not** scan published output. The .NET WebAssembly runtime calls
     `crypto.getRandomValues` itself, so a scan over `publish/wwwroot` fails for a reason
     that is not a defect in this repository.
   - Exclude the guard test's own file. It necessarily contains every string it searches
     for.

   A future need for randomness (there is none in scope) does not get an exemption in the
   test. It gets a discussion first.
2. **`CSharpFunctionalExtensions` is the only permitted NuGet dependency, and its carve-out
   is deliberate.** It supplies `Result` and nothing else: no cryptography, no I/O, no
   network, no reflection over user data, and it is small enough to read. That is why it
   survives the rule in the opening paragraph. Nothing else does.

   In particular: no cryptographic dependency beyond `System.Security.Cryptography.SHA256`.
   The conversion is SHA-256, a truncation, a checksum and an 11-bit split. Implement BIP-39
   directly. Do not add NBitcoin or any wallet library: the point of this app is to be
   independent of other implementations, and a large dependency makes it unreadable without
   making it more correct.
3. **No BIP-32, no secp256k1, no addresses, no fingerprints.** Explicit non-goals. See the
   plan for the reasoning.
4. **Both dialects are first-class, and both carry vectors.** Identical physical rolls
   produce different seeds under different vendors' conventions, and no device announces
   which one it uses. Coldcard, SeedSigner and Ian Coleman set to Base 10 hash `15634`;
   Krux hashes `1-5-6-3-4`. A tool that supports one dialect silently tells a user of the
   other that their device is wrong, which is precisely the failure this app exists to
   distinguish from a real one.

   Therefore: the separator is a visible choice labelled with the devices it matches, the
   exact preimage is always rendered character for character, and the test suite carries
   vectors for **both** conventions. A dialect whose vectors have not been confirmed against
   that vendor's own published output is not offered as a selectable option; see the open
   verification items in the plan.
5. **Every algorithmic change re-runs the published vectors.** The official BIP-39 English
   vectors and the dice vectors for both dialects are in the test suite. A change that moves
   any of them is wrong until proven otherwise.
6. **No persistence, no network, no telemetry, no clipboard writes of seed material.** The
   app must work with the network cable out, and must leave nothing behind. No
   `localStorage`, no `sessionStorage`, no cookies, no analytics, no external fonts or CDNs.
7. **The wordlist is verified at runtime** against its known SHA-256, and the app refuses to
   derive anything if the check fails.
8. **Tails first: the shipped artifact is a download, not a website.** The intended way to
   run this app is to publish it, copy `wwwroot` to a USB stick, boot Tails with networking
   off, serve it with `python3 -m http.server 9876 --bind 127.0.0.1`, and open
   `http://127.0.0.1:9876` in LibreWolf. A local server is required because Blazor
   WebAssembly will not load over `file://`; there is no file-and-double-click option, and
   any instruction that implies one is wrong.

   If a build is ever served from anywhere other than `127.0.0.1` or `localhost`, it must
   say so at the top of the page, before the input, in the manner of the sibling
   `slip39-backup` README: this build is a demonstration and a real roll log must not be
   typed into it. A roll log is the seed, in plaintext, before any hashing. Treat a hosted
   build of this app as more dangerous than a hosted build of a tool that splits a seed the
   user already holds.
9. **Validation failures are `Result`, not exceptions.** Use `CSharpFunctionalExtensions`.
   A short roll log or a stray character is an expected input, not an exceptional condition.
   Exceptions are for things that should not happen.

## C# conventions

- `private` is implied; do not write it.
- Prefer expressions over method-bodied returns. One-liners use `=>`, never a braced
  `return`.
- Omit braces on an `if` when it can be avoided.
- Immutable by default. Setter preference order, most preferred first: no setter, then
  `private init`, then `private set`, then `init`, then `set`.
- Read-only collection types in public surfaces.
- `var` over explicit types.
- Functional style. Prefer LINQ and expression-bodied members over loops.

## Writing style

No em dashes and no en dashes. Use a colon, semicolon, comma, parentheses, or a sentence
break instead. This applies to prose, comments, commit messages and UI copy.

Do not use self-describing rhetoric: state the fact rather than commenting on how the prose
delivers it.

## Attribution

Commits, pull requests and issues do not carry "Generated with Claude Code" or a
`Co-Authored-By` trailer. Where attribution is wanted, add a small italic
*Collaboration by Claude* line instead.
