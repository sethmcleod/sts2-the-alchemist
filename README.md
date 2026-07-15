# The Alchemist

> An amphibian practicer of esoteric arts.
> Transmutes self-inflicted poison into power.

## Overview

The Alchemist is a high-risk, high-reward character defined by one loop:
deliberately run your own health low to unlock your strongest effects, then
convert that danger back into survival. A constant balance between poisoning
yourself and healing yourself.

Three overlapping archetypes give the class its shape:

* **The Transmuter** — *turn self-inflicted Poison into fuel.* Poison yourself
  as readily as your enemies, convert it into Regen, then spend that Regen on
  damage, Block, and Strength. At or below half health, your Gambit cards gain an
  extra effect — the archetype that thrives one bad decision from disaster.
* **The Distiller** — *make your hand a resource managed over time.* Ferment
  cards grow stronger every turn you hold them; Seep cards punish you for waiting.
  Patience versus commitment, weighed every turn.
* **The Apothecary** — *build an engine from cards and potions.* Infuse the cards
  you already love to enchant them mid-combat, refine your hand into Distillates,
  and keep a full potion belt that several cards cash straight into damage, Block,
  and tempo.

## Content

* A new character meticulously designed to fit in with the rest
* 95 cards, including 4 multiplayer cards and 2 full-art Ancient rewards
* 9 unique relics
* 3 unique potions
* 3 unique enchantments
* A 7-epoch timeline inspired by the Great Work
* Many lines of unique dialog with Ancients and other characters

## Mechanics and Keywords

* **Poison & Regen**: The Alchemist deliberately self-poisons, then converts
  Poison into Regen (and Regen into damage, Block or Strength) — two resources
  meant to be actively cycled, not passively endured.
* **Gambit**: "Gambit: [effect]" triggers only while your HP is at or below 50%
  of your Max HP. The card works normally above half health — Gambit is a bonus
  for playing dangerously, never a requirement.
* **Ferment**: Found on cards that also Retain. A Fermenting card's effect grows
  in potency for every turn it stays unplayed in your hand — no cap, but every
  turn held is a turn it occupies your hand. Draft these in moderation.
* **Seep**: "Seep: [effect]" resolves at the end of your turn only if the card
  is still in your hand. Most Seep effects are costs that pressure you to play
  the card; a few are small benefits. Seep is Ferment's mirror — one rewards
  patience, the other discourages it.
* **Infuse**: Infusing a card enchants it for the rest of combat based on its
  type — Attacks gain **Sharp**, Skills gain **Nimble** or **Adroit**, Powers
  gain **Swift**, and Status/Curse/Quest cards gain **Ethereal**. Same-type
  infusions can stack
  multiple times.

## A Note on Quality

The Alchemist has been lovingly crafted to feel like a natural addition to the
game. Every card, relic, and potion has been balanced against the existing pools
across repeated low and high-ascension playtesting. The same care extends to
flavor: this character's story is woven directly into the game's existing
timeline and characters, and observant players will find more than a few threads
connecting them to the world. All artwork is in the process of being hand-drawn
and animated in the style of the base game.

## Getting Started

### Play it

> [!NOTE]
> No packaged release yet — playing means building from source (below).
> `scripts/dev.sh publish` installs the mod straight into your game.
> I intend to publish this to Steam Workshop once the initial character art is
> ready.

Requires Slay the Spire 2 (Steam) and the prerequisites in [BUILD.md](BUILD.md).

### Develop

```sh
git clone https://github.com/sethmcleod/sts2-the-alchemist && cd sts2-the-alchemist
scripts/setup.sh          # one-time: tooling checkout, dependency check, bridge mods
                          #   (bridge mods = small game-side mods the test suite talks to)
scripts/dev.sh publish    # build the mod into the game
# launch Slay the Spire 2 via Steam (use a spare save profile), then:
scripts/dev.sh test       # regression suite against the live game
```

`scripts/dev.sh doctor` diagnoses the environment at any point.

> [!TIP]
> Everything works with plain shell + Python — **no AI tooling required**. But the repo
> is also set up for agent-assisted development ([CLAUDE.md](CLAUDE.md)): even without C#
> experience, you can point Claude Code (or similar) at this repo to modify and test the mod.

### Where things are

| Doc | What's in it |
|---|---|
| [BUILD.md](BUILD.md) | prerequisites, build/publish commands, asset conventions |
| [CONTRIBUTING.md](CONTRIBUTING.md) | the three-way update rule, design + code conventions |
| [docs/adding-a-card.md](docs/adding-a-card.md) | end-to-end worked example: add one card |
| [docs/troubleshooting.md](docs/troubleshooting.md) | known gotchas and their fixes |
| [scripts/tests/README.md](scripts/tests/README.md) | regression suite: running + authoring |
| [sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) | the general STS2 modding toolkit this repo builds on |