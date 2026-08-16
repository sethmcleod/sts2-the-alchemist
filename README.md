<img width="450" height="90" alt="header" src="https://github.com/user-attachments/assets/ca040185-202a-4fcd-b4fe-39a16cc813ba" />

## Overview

- ✨ 90+ new cards
- 💎 9 new relics
- 🧪 9 new potions
- 📚 7 timeline epochs
- 🎨 Handmade art and animation _(WIP)_
- 🌍 Translated into 15 languages
- 🐸 You get to be a frog

## Playstyle

- **Antitoxin**: A reserve that absorbs the damage Poison would deal to you, and
  cures that much Poison in the process. It persists across turns, up to a limit.
- **Absorbing Pays Off**: Taking Poison damage is not just a cost. Several cards
  reward it, turning the Poison you absorb into Strength, Block, Energy or damage.
- **Infuse**: Infusing a card Enchants it for the rest of combat based on its
  type. Attacks apply Poison, Skills generate Antitoxin, Powers give Strength,
  and other cards gain Ethereal.
- **Ferment**: These effects grow in potency for every turn the card stays in
  your hand. Playing the card transforms it into a Toxic.
- **Brew**: This Rest Site option allows you to procure one of 6 unique potions
  that can only be obtained this way.
- **Sell Potions**: Due to the quality of these brews, the Merchant is willing
  to buy them from you for Gold.

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
