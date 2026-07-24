# Contributing to The Alchemist

[BUILD.md](BUILD.md) has the setup steps and the build commands. To make a card, copy the
closest existing card in `AlchemistCode/Cards/` and follow the three-way rule below.

## The three-way update rule

> [!IMPORTANT]
> A card is in three files. The three files must agree. If you change one file, change all
> three files. If you change a number in the code, but not in the localization and
> `cards.csv`, the three files no longer agree. The command `scripts/dev.sh lint` checks
> the three files offline.

1. **Code**: the card class in `AlchemistCode/Cards/`. Each number is in a builder pair
   with the format `WithDamage(base, upgradeDelta)`.
2. **Localization**: `Alchemist/localization/eng/*.json`. Use `{Var:diff()}` tokens. Do
   not write numbers in the text. The tokens let the game show the upgrade preview. Each
   power also needs the keys `.title`, `.description`, and `.smartDescription`.
3. **cards.csv**: the design sheet in plain text. The format is `base (upgraded)`.

## Design conventions

- **Show real numbers.** A conditional value, or a value that scales, must show its
  current total in green. Use `WithCalculatedDamage` or `WithCalculatedBlock`. You can
  also use a text in parentheses, as Steep does with "(Applies N Poison.)". The player
  must never calculate a value.
- **An upgrade never increases the cost of a card.**
- **Risk and reward**: a card with a number above the curve must have a cost. The
  cost is HP, or Poison on the player. Single target damage is usually below the base game
  curve. Poison damage increases over time and makes up for the difference.

## Code style

- **A comment must tell why, not what.** The names and the structure must make the _what_
  clear. Write a comment only where a reader can become confused. These are the usual
  reasons:
  - a workaround for a problem in the base game
  - a dependency on the order of operations
  - a reflection hack
  - a choice that looks incorrect, where a "fix" would break the code

  Keep each comment short. Do not write TODO comments. Do not write notes about removed
  code or previous code. Do not write API doc blocks by default.

- Put each Harmony patch in `AlchemistCode/Patches/`. Use one file for each concern. Start
  each file with a `//` header. The header must explain the engine constraint.
  `MainFile.Initialize` keeps a patch failure inside one class. Thus you must never assume
  that another patch ran.
- Keep the mod assets in `res://Alchemist/`. Never put them at base game `res://` paths.
  [BUILD.md](BUILD.md) gives the reason.
- Put state that must survive a save and a reload in `DynamicVars`. Do not use plain
  fields.
- Description code and tooltip code must not read `Owner` without a guard. A canonical
  model (in the compendium) throws an exception. Use `IsMutable` as the guard (see
  `AlchemistCard.ShouldGlowGoldInternal`).

## Testing

`dotnet build` must pass with 0 errors. The localization analyzer runs as part of the
build. After a change to the gameplay, test the card in the game. Test the base card and
the upgraded card. For a power, also test more than one stack.

## Changelog & releases

Each change that a player can see needs an entry in [CHANGELOG.md](CHANGELOG.md) under
`## [Unreleased]`. This includes content, balance, a bug fix, text, and art. Write the
entry for players. [RELEASING.md](RELEASING.md) has the version policy and the release
procedure.
