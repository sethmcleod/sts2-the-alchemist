# The Alchemist

> An amphibian practicer of esoteric arts.
> Transmutes self-inflicted poison into power.

## Overview

The Alchemist is a high-risk, high-reward character. The core loop is to
deliberately run your health low to unlock your strongest effects, then convert
that danger back into survival. You're always balancing poisoning yourself
against healing yourself.

Three overlapping archetypes give the class its shape:

- **The Transmuter**: turn self-inflicted Poison into fuel. Poison yourself as
readily as your enemies, convert it into Regen, then spend that Regen on
damage, Block, and Strength. At or below half health your Gambit cards gain an
extra effect, so this archetype thrives one bad decision from disaster.
- **The Distiller**: make your hand a resource managed over time. Ferment cards
grow stronger every turn you hold them, and Seep cards punish you for waiting.
Patience versus commitment, weighed every turn.
- **The Apothecary**: build an engine from cards and potions. Infuse the cards
you already love to enchant them mid-combat, refine your hand into Distillates,
and keep a full potion belt that several cards cash straight into damage,
Block, and tempo.



## Content

- A new character meticulously designed to fit in with the world
- 95 cards, including 4 multiplayer cards and 2 full-art Ancient rewards
- 9 unique relics
- 3 unique potions
- 3 unique enchantments
- A 7-epoch timeline inspired by the Great Work
- Many lines of unique dialog with Ancients and other characters
- Hand-drawn artwork for all cards, relics, potion, and icons (WIP)



## Mechanics and Keywords

- **Poison & Regen**: The Alchemist deliberately self-poisons, then converts
Poison into Regen (and Regen into damage, Block or Strength). Both are meant to
be actively cycled, not passively endured.
- **Gambit**: "Gambit: [effect]" triggers only while your HP is at or below 50%
of your Max HP. The card works normally above half health. Gambit is a bonus
for playing dangerously, never a requirement.
- **Ferment**: Found on cards that also Retain. A Fermenting card's effect grows
in potency for every turn it stays unplayed in your hand. There's no cap, but
every turn held is a turn it occupies your hand. Draft these in moderation.
- **Seep**: "Seep: [effect]" resolves at the end of your turn only if the card
is still in your hand. Most Seep effects are costs that pressure you to play
the card, though a few are small benefits. Seep is Ferment's mirror: one
rewards patience, the other discourages it.
- **Infuse**: Infusing a card enchants it for the rest of combat based on its
type. Attacks gain 2 **Toxic** (apply Poison on unblocked damage), Skills gain 1
**Fuming** (add Foul Vapor to your Hand), Powers gain 1 **Exalted** (gain
Strength), and Status/Curse/Quest cards gain **Ethereal**. Same-type infusions
stack, each one adding that much again.



## A Note on Quality

The Alchemist has been lovingly crafted to feel like a natural addition to the
game. Every card, relic, and potion has been balanced against the existing pools
across repeated low and high-ascension playtesting. The same care extends to
flavor: this character's story is woven directly into the game's existing
timeline and characters, and observant players will find more than a few threads
connecting them. ✨

## How to start



### Play it

**Steam Workshop** is the best way to install the mod and play it. It needs one
click. It keeps the mod updated. It also installs the necessary BaseLib
dependency for you.

> [!NOTE]
> The Workshop release comes after the character artwork is complete. Until then,
> use the manual installation below.

**Manual install:**

1. Install **[BaseLib](https://steamcommunity.com/workshop/filedetails/?id=3737335127)**
  first. The Workshop does this step for you.
2. Download `Alchemist-vX.Y.Z.zip` from the
  [Releases](https://github.com/sethmcleod/sts2-the-alchemist/releases) page.
3. Extract the `Alchemist/` folder into the `mods/` folder of your game.

[RELEASING.md](RELEASING.md#how-players-install-it) gives the full steps and the
`mods/` path for each platform. You do not need to clone the repo. You do not
need build tools.

The mod needs Slay the Spire 2 on Steam. To build from source, read the
prerequisites in [BUILD.md](BUILD.md), then use the Develop steps below.

### Develop

```sh
git clone https://github.com/sethmcleod/sts2-the-alchemist
cd sts2-the-alchemist
scripts/setup.sh          # first time: get the tooling, check dependencies, install bridge mods
scripts/dev.sh test       # run the regression suite against the live game
scripts/dev.sh publish    # build the mod into the game
```

The command `scripts/dev.sh doctor` checks the environment at any time.

> [!TIP]
> Everything works with a plain shell and Python, so **no AI tooling is necessary**. The
> repo also supports development with an agent (see [CLAUDE.md](CLAUDE.md)). You can point
> Claude Code, or a similar tool, at this repo to change the mod and to test it. This works
> even if you have no experience with C#.



### Document map


| Doc                                                                | What is in it                                                            |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------ |
| [BUILD.md](BUILD.md)                                               | prerequisites, build and publish commands, asset rules                   |
| [CONTRIBUTING.md](CONTRIBUTING.md)                                 | the three-way update rule, design and code rules                         |
| [RELEASING.md](RELEASING.md)                                       | version policy, changelog workflow, how to cut a release, how to install |
| [docs/adding-a-card.md](docs/adding-a-card.md)                     | a complete example that adds one card                                    |
| [docs/troubleshooting.md](docs/troubleshooting.md)                 | known problems and their fixes                                           |
| [docs/backlog.md](docs/backlog.md)                                 | improvements with no schedule, and the evidence for each                 |
| [scripts/tests/README.md](scripts/tests/README.md)                 | the regression suite: how to run it and how to write a scenario          |
| [sts2-modding-mcp](https://github.com/sethmcleod/sts2-modding-mcp) | the general STS2 modding toolkit for this repo                           |


