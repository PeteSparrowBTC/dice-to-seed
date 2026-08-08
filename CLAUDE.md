# Working in this repository

This repository is a single offline Blazor WebAssembly app that turns a log of six-sided
dice rolls into a BIP-39 seed phrase. Its whole reason to exist is to be a **second,
independent implementation** that a Coldcard, a SeedSigner or Ian Coleman's tool can be
checked against. That purpose sets the engineering rules: the conversion must be small
enough to read in one sitting, it must carry published test vectors, and it must never
acquire a dependency whose behaviour cannot be verified from the repository itself.

The app derives keys that will hold real money. Treat every change as a change to
key-generation code.

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

1. **No cryptographic dependency beyond `System.Security.Cryptography.SHA256`.** The
   conversion is SHA-256, a truncation, a checksum and an 11-bit split. Implement BIP-39
   directly. Do not add NBitcoin or any wallet library: the point of this app is to be
   independent of other implementations, and a large dependency makes it unreadable without
   making it more correct.
2. **No BIP-32, no secp256k1, no addresses, no fingerprints.** Explicit non-goals. See the
   plan for the reasoning.
3. **Every algorithmic change re-runs the published vectors.** The official BIP-39 English
   vectors and the Coldcard-published dice examples are in the test suite. A change that
   moves any of them is wrong until proven otherwise.
4. **No persistence, no network, no telemetry, no clipboard writes of seed material.** The
   app must work with the network cable out, and must leave nothing behind. No
   `localStorage`, no `sessionStorage`, no cookies, no analytics, no external fonts or CDNs.
5. **The wordlist is verified at runtime** against its known SHA-256, and the app refuses to
   derive anything if the check fails.
6. **Validation failures are `Result`, not exceptions.** Use `CSharpFunctionalExtensions`.
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
