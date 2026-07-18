---
name: card
description: Add a new card to the Alchemist mod, or change an existing card's numbers, cost, text, or mechanics. Use this whenever a card is being created or edited, since a card lives in three files that must stay in sync (the C# class, the localization JSON, and cards.csv) and a change to one without the others is how the mod drifts out of sync. Covers the full loop through lint, publish, and a regression scenario.
---

# Add or change a card

A card is defined in **three places that must stay in sync**. Touch one, touch all:

1. **Code**: the card class under `AlchemistCode/Cards/<Rarity>/<Name>.cs`, where every
   numeric value lives as a `(base, upgradeDelta)` builder pair.
2. **Localization**: `Alchemist/localization/eng/cards.json`, the on-card text.
3. **cards.csv**: the plain-text design sheet, `Card,Rarity,Type,Cost,Description`.

The offline `scripts/dev.sh lint` enforces this three-way sync, so it is the fast check
that you did it right.

The full worked example, adding one real card start to finish, is
[docs/adding-a-card.md](../../../docs/adding-a-card.md). Read it once; this skill is the
checklist on top of it. The rules behind the conventions are in
[CONTRIBUTING.md](../../../CONTRIBUTING.md).

## Adding a card

1. **Pick the display name first.** The class name derives everything else mechanically:
   `DoubleDose` → model id `ALCHEMIST-DOUBLE_DOSE` → portrait `double_dose.png` → loc keys
   `ALCHEMIST-DOUBLE_DOSE.title` / `.description`. Get the name right before writing files.
2. **Copy an existing card** from the same rarity folder as the starting point. The
   `using` imports and `namespace` line must match the folder, and copying gets them right
   for free. Keep every number in the constructor's `With*` builders (`WithDamage(3, 1)`),
   never hardcoded in `OnPlay`.
3. **Add the two loc keys** to `cards.json` in alphabetical order. Use `{Var:diff()}`
   tokens so the upgrade preview renders; never hardcode a number that exists as a builder.
   Wrap keyword and zone names in `[gold]…[/gold]` and add a `\n` between sentences. Match
   base-game wording (see the card-text conventions in CONTRIBUTING).
4. **Add the cards.csv row**, format `base (upgraded)`, plain text with no `[gold]` tags.
   Quote the description if it contains a comma.
5. **Add the portrait**: a 1000×760 PNG at
   `Alchemist/images/card_portraits/big/<name>.png`. Copy any existing portrait as a
   placeholder while prototyping.

## Changing an existing card

Find the three homes for the card and change all of them together:
- the number/text in the card class builders,
- the matching `cards.json` keys,
- the `cards.csv` row.

If the change alters behavior or numbers, its **regression scenario must change too** (see
below). A number change with no test change is how a regression ships.

## Verify

Run these in order. Do not stop at a green build; the loc and csv only ship through the
pck and the lint.

```sh
scripts/dev.sh lint       # three-way sync + numeric cross-check, offline, fast
scripts/dev.sh publish    # build → import → publish → verify pck
```

Then confirm in the live game. Spawn the card from the dev console with its **full model
id**, not the display name (bare names silently no-op):

```
card ALCHEMIST-DOUBLE_DOSE
```

Driving the game safely (fresh process, Profile 3, restoring the menu afterward) is the
**playtest** skill's job. Use it rather than reinventing the loop.

## Lock it in with a test

Copy the closest scenario in `scripts/tests/cards/` and adjust it: spawn the card, play
it, assert the outcome. Scenarios are small JSON files (`do`/`expect`, polled until true).
The assertion vocabulary and the bridge quirks are in
[scripts/tests/README.md](../../../scripts/tests/README.md).

```sh
scripts/dev.sh test <name>        # run just your scenario while iterating
```

A new or changed card is not done until `lint` passes, the game shows it correctly, and a
scenario covers it.
