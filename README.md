# The Alchemist

> An amphibian practicer of esoteric arts.
> Transmutes self-inflicted poison into power.

## Overview

The Alchemist is a new character with a high-risk, high-reward playstyle. You'll
benefit from staying below 50% HP, but also be equipped with tools to gain that
health back through Regen and potions. You can Enchant cards in combat to give
them powerful effects, multiple times. You can also brew potions at rest sites
and, more importantly, _sell them to the Merchant!_ 💰

_(Disclaimer: This description was paid for by the Merchant.)_

## Content

- 🐸 A new character meticulously designed to fit in with the rest of the game
- ✨ 97 new cards, including 4 multiplayer cards and 2 full-art Ancient rewards
- 💎 9 new relics
- 🧪 6 new potions
- 🪄 3 new enchantments
- 📚 7 timeline epochs inspired by the hermetic Great Work
- 💬 Loads of dialog with Ancients and other characters
- 🎨 Hand-drawn artwork for all cards, relics, potions, and icons _(WIP)_

## Mechanics and Keywords

- **Poison & Regen**: The Alchemist deliberately self-poisons, then converts
  Poison into Regen, and Regen into damage, Block or other useful effects. Both
  are meant to be actively cycled, not passively endured.
- **Gambit**: Cards with this keyword have buffs that are only active while your
  HP is 50% or less. Whether increasing damage or adding additional effects,
  you'll benefit from playing strategically.
- **Ferment**: This keyword is only found on cards that also Retain. These
  effects grow in potency for every turn the card stays in your hand, and
  persist after the card is played.
- **Seep**: Cards with this keyword have a green glow and will trigger an effect
  if still in your hand at the end of turn. Most are costs that pressure you to
  play the card, though some are beneficial.
- **Infuse**: Infusing a card Enchants it for the rest of combat based on the
  type. Attacks apply Poison, Skills create tokens that apply Weak and
  Vulnerable, but Poison you, Powers give Strength, and other cards gain
  Ethereal.
- **Brew**: This new Rest Site option allows you to remove a card from your deck
  and procure a random potion. There are 3 new potions that can only be obtained
  this way.
- **_Sell Potions!_**: Due to the quality and potency of these brews, the
  Merchant is willing to buy potions from you, offering Gold based on the
  rarity.

## A Note on Quality

The Alchemist has been lovingly crafted to feel like a natural addition to the
game. Every card, relic, and potion has been (and will continue to be) balanced
against the existing pools through many rounds of low and high-Ascension
playtesting. The same care extends to flavor and lore: this character's story is
woven directly into the game's existing timeline, and observant players will
find more than a few threads connecting them to the world.

Enjoy! 🧙‍♂️

---

## Play it

**Steam Workshop** is the best way to install and play... soon!

> [!NOTE]
> There will be a Workshop launch as soon as the first round of character
> artwork is complete. Until then, you can use the manual installation below.

**Manual install:**

1. Install
   **[BaseLib](https://steamcommunity.com/workshop/filedetails/?id=3737335127)**
   which is a framework most mods depend on (you may already have it installed)
2. Download `Alchemist-vX.Y.Z.zip` from the
   [Releases](https://github.com/sethmcleod/sts2-the-alchemist/releases) page.
3. Extract the `Alchemist/` folder into the `mods/` folder of your game:

- **macOS**: `…/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/`
- **Windows/Linux**: the `mods/` folder in the same location as the game
  executable.

## Develop

First off, check out the [CONTRIBUTING.md](CONTRIBUTING.md) guide.

This repo assumes you have Slay the Spire 2 connected through Steam. To build
from source, read the prerequisites in [BUILD.md](BUILD.md), then use the
Develop steps below.

```sh
git clone https://github.com/sethmcleod/sts2-the-alchemist
cd sts2-the-alchemist
scripts/dev.sh doctor     # check the environment
scripts/dev.sh publish    # build the mod into the game
```

### Document map

| Doc                                                | What is in it                                                            |
| -------------------------------------------------- | ------------------------------------------------------------------------ |
| [BUILD.md](BUILD.md)                               | prerequisites, build and publish commands, asset rules                   |
| [CONTRIBUTING.md](CONTRIBUTING.md)                 | the three-way update rule, design and code rules                         |
| [RELEASING.md](RELEASING.md)                       | version policy, changelog workflow, how to cut a release, how to install |
