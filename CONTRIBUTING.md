# Contributing to The Alchemist

Setup and build commands live in [BUILD.md](BUILD.md). For a card start to finish, follow
the worked example in [docs/adding-a-card.md](docs/adding-a-card.md).

> [!TIP]
> If you work with Claude Code or a similar agent, `.claude/skills/` has skills that drive
> these workflows: `card` (add or change a card), `balance-sync` (apply a design pass from
> an updated cards.csv), and `playtest` (run the mod against the live game safely).
> Everything below is doable by hand without them.

## The three-way update rule

> [!IMPORTANT]
> A card lives in three files that must stay in sync, so a change to one means a change to
> all three. A number changed in code with no matching loc/csv update is how the mod drifts
> out of sync. `scripts/dev.sh lint` checks this offline.

1. **Code**: the card class under `AlchemistCode/Cards/`, where every number lives as a
   `WithDamage(base, upgradeDelta)`-style builder pair.
2. **Localization**: `Alchemist/localization/eng/*.json`. Use `{Var:diff()}` tokens rather
   than hardcoded numbers so upgrade previews render. Powers also need `.title`,
   `.description`, and `.smartDescription`.
3. **cards.csv**: the plain-text design sheet, format `base (upgraded)`.

## Design conventions
- **Show real numbers.** Any conditional or scaling value shows its live total in green
  (via `WithCalculatedDamage`/`WithCalculatedBlock`, or a parenthetical like Steep's
  "(Applies N Poison.)"). Players never do mental math.
- **Upgrades never increase cost.**
- **Risk/reward pricing**: above-curve numbers are paid for with HP or self-Poison taxes.
  Single-target damage trends below base-game curves, since poison scaling compensates.

## Code style
- **Comment why, not what.** The *what* should be clear from naming and structure. Reach for
  a comment only where a reader would otherwise be misled or lost: a base-game quirk being
  worked around, a subtle ordering dependency, a reflection hack, or a counterintuitive
  choice that invites a "fix" which would break things. Keep them brief. No TODOs, no notes
  about removed or previous code, no API doc blocks by default.
- Harmony patches live in `AlchemistCode/Patches/`, one concern per file, opening with a
  `//` header that explains the engine constraint being worked around. `MainFile.Initialize`
  isolates patch failures per class, so never assume another patch ran.
- Keep mod assets under `res://Alchemist/`, never at base-game `res://` paths (see
  [BUILD.md](BUILD.md) for why).
- State that must survive save/reload goes in `DynamicVars`, not plain fields.

## Testing
`dotnet build` must pass with 0 errors (the loc analyzer runs in-build). For gameplay
changes, check the card in-game base and upgraded (and stacked, for powers).

The repo also ships an automated regression suite that runs against the live game, no agent
tooling required. Run `scripts/dev.sh test` before a PR that touches card or power behavior,
and **add or update a scenario** for any card whose numbers or mechanics change. Running and
authoring scenarios is covered in [scripts/tests/README.md](scripts/tests/README.md).

## Changelog & releases
Every player-visible change (content, balance, bug fix, text, art) gets an entry under
`## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md), worded for players. The versioning policy
and release process live in [RELEASING.md](RELEASING.md).
