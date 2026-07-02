# Contributing to The Alchemist

Thanks for helping brew! Setup and build commands live in [BUILD.md](BUILD.md).

## The three-way update rule
A card is defined in three places that must stay in sync — PRs that touch one must touch all:
1. **Code** — the card class (`AlchemistCode/Cards/...`), where all numeric values live (builders like `WithDamage(base, upgradeDelta)`).
2. **Localization** — `Alchemist/localization/eng/*.json`. Use `{Var:diff()}` tokens, never hardcoded numbers, so upgrade previews render. Powers need `.title`, `.description` **and** `.smartDescription`.
3. **cards.csv** — the human-readable design sheet, format `base (upgraded)`.

## Design conventions
- **Show real numbers.** Any conditional/scaling value must display its live total in green (use `WithCalculatedDamage`/`WithCalculatedBlock`, or a parenthetical like Steep's "(Applies N Poison.)"). Players never do mental math.
- **Upgrades never increase cost.**
- **Risk/reward identity**: above-curve numbers are priced with HP/self-Poison taxes; single-target damage trends *below* base-game curves (poison scaling compensates).
- Card display names may use British spellings (Materialise, Catalyse); class/asset/loc identifiers always match the display name exactly.

## Code style
- Comments only where code is genuinely non-obvious (Harmony patches, engine workarounds, mechanic subtleties) — explain *why*, not *what*.
- Harmony patches live in `AlchemistCode/Patches/`, one concern per file, with a `///` summary explaining the engine constraint being worked around. Patch failures are isolated per class by `MainFile.Initialize` — never assume another patch ran.
- Never place mod assets at base-game `res://` paths; keep everything under `res://Alchemist/`.
- Power/card state that must survive save/reload goes in `DynamicVars`, not plain fields.

## Testing
`dotnet build` must pass with 0 errors (the loc analyzer runs in-build). For gameplay changes, verify in-game: play the card base + upgraded, and stacked base+upgraded for powers.
