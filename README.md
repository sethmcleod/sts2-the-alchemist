<img width="450" height="90" alt="header" src="https://github.com/user-attachments/assets/ca040185-202a-4fcd-b4fe-39a16cc813ba" />

## Overview

- ✨ 90+ new cards
- 💎 9 new relics
- 🧪 6 new potions
- 📚 7 timeline epochs
- 🎨 Handmade art and animation _(WIP)_
- 🌍 Translated into 15 languages
- 🐸 You get to be a frog

## Playstyle

- **Antitoxin**: A reserve that absorbs the damage Poison would deal to you. It
  caps at 9 and persists across turns. Anything past the cap becomes Block, so a
  full reserve is never wasted.
- **Payoffs**: Several cards trigger when Antitoxin absorbs Poison damage,
  fueling Strength, Block, Energy, or damage to enemies.
- **Ferment**: These effects grow in potency for every turn the card stays in
  your hand. However if you Retain them past their peak, they spoil into Toxic
  cards.
- **Infuse**: Infusing a card Enchants it for the rest of combat based on the
  type. Attacks apply Poison to the enemy and to you, Skills generate Antitoxin,
  Powers give Strength, and other cards gain Ethereal.
- **Brew**: This new Rest Site option lets you procure a random potion.
  There are 3 new potions that can only be obtained this way.
- **_Sell Potions!_**: Due to the quality and potency of these brews, the
  Merchant is willing to buy potions from you, offering Gold based on the
  rarity!

## Disclaimer

The Alchemist has been lovingly crafted to feel like a natural addition to the
game. Every card, relic, and potion has been (and will continue to be) balanced
against the existing game. The same care extends to flavor and lore: this
character's story is woven directly into the game's existing timeline, and
observant players will find more than a few threads connecting them to the
world.

This mod is still in it's early stages and content is subject to change, but
feedback is welcome! The best place to discuss the mod is official Slay the Spire
Discord server in the #modding-forum.

## Credits

Design and Code - Seth\
Art and Animation - [Fulgur](https://fulgur.carrd.co/)

---

## Play it

> [!NOTE]
> Steam Workshop is the best way to install and play, since it will
> automatically prompt you to install BaseLib:
> https://steamcommunity.com/sharedfiles/filedetails/?id=3780726901
> Nexus Mods is also an option:
> https://www.nexusmods.com/slaythespire2/mods/1439?tab=description

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

| Doc                                | What is in it                                                            |
| ---------------------------------- | ------------------------------------------------------------------------ |
| [BUILD.md](BUILD.md)               | prerequisites, build and publish commands, asset rules                   |
| [CONTRIBUTING.md](CONTRIBUTING.md) | the three-way update rule, design and code rules                         |
| [RELEASING.md](RELEASING.md)       | version policy, changelog workflow, how to cut a release, how to install |
