# Changelog

This document lists all the important changes that players see in The Alchemist.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). This
project also follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see [RELEASING.md](RELEASING.md) to know what each version increase means for a
mod). Each version section below is also the Steam Workshop update note for that
version.

The sections follow Keep a Changelog: Added, Changed, Deprecated, Removed, Fixed,
Security.

## [Unreleased]

### Added

- Added a placeholder Alchemist character-select splash, replacing the borrowed
  Ironclad backdrop until the final art is ready

### Changed

- Reworked Anneal card and renamed it Quench: "Draw a card. Infuse a card in
  your Hand. If this card is Enchanted, gain 2 (3) Regen." -> "Draw 1 (2) cards.
  Infuse a card in your Hand. If this card is Enchanted, Infuse an additional
  card."
- Changed Amalgam card: the X in its description now shows as X+N, where N counts
  up with each turn fermented, and adds 1 more when upgraded
- Nerfed Poultice card: Regen decreased from 2 (3) -> 1 (2)
- Nerfed Zenith card: cost increased from 2 -> 3 (2), and it now always Doubles
  instead of Tripling when Upgraded
- Changed Tinge card upgrade: damage now increases 3 -> 5 instead of 3 -> 4, and
  Seep Regen stays at 1 instead of increasing to 2
- Changed Trickle card upgrade: now draws 2 cards instead of dealing more damage,
  and Seep Regen stays at 1 instead of increasing to 2
- Changed Catalyze card upgrade: Regen stays at 2, and the upgrade still reduces
  cost 2 -> 1
- Nerfed Fester card: cost increased from 0 -> 1
- Reworked Double Dose card: "Deal 4 (5) damage twice. If this card is
  Enchanted, apply 2 Weak." -> "Deal 4 (5) damage twice. Each hit applies
  1 (2) Poison."
- Buffed Spatter card: each hit now also applies 1 Poison to the enemy it hits
- Buffed Fighting Spirits card: upgraded damage increased from 12 -> 14
- Changed Fighting Spirits card: it now glows and shows the potions used this
  combat in green, like other scaling cards
- Cards that gain an effect from being Enchanted now glow gold in the Infuse
  selection, so you can see the best cards to Infuse
- Fumigate card upgrade now shows its base damage increase 1 -> 2 on the card
  sheet (the upgrade already did this)
- Buffed Carapace card: Block per turn fermented is now a flat 6 (9), up from
  50% (75%) of base Block
- Buffed Rolling Boil card: damage per turn fermented is now a flat 4 (6) per
  hit, up from 50% (75%) of base damage
- Buffed Patient Strike card: damage per turn fermented is now a flat 6 (9), up
  from 75% (100%) of base damage
- Buffed Sinter card: free-cost condition decreased from 7 -> 5 cards in your
  Exhaust Pile

### Fixed

- Fixed a potential crash when the game renders the Gold Leaf potion outside a
  run, such as in the Potion Lab
- Fixed Glowing Shard relic showing the Accelerant power icon on you and allies.
  It no longer grants the Accelerant power; it adds the extra enemy Poison
  trigger directly

## [0.3.0] - 2026-07-20

### Added

- Added 3 Brew-only potions, offered only when you Brew (never from shops,
  rewards, or random generation) at roughly 1 in 3 Brews:
  - Quicksilver Draught: "Take an extra turn after this one. Draw no cards at
    the start of it."
  - Basilisk Bile (thrown): "Trigger Poison on ALL enemies 2 times."
  - Alkahest: "If used in combat, Infuse up to 3 cards in your Hand. Otherwise,
    upgrade a card in your Deck."
- Added Venom Trance card (Uncommon Skill, cost 1): "If an enemy has 8 (6) or
  more Poison, take an extra turn after this one. Exhaust."

### Removed

- Removed Transpose card, replaced by Venom Trance

### Changed

- Buffed Aqua Vitae relic: now also grants 1 Regen whenever you use a potion
- Buffed Gold Leaf potion: now also grants 1 Block for every 15 Gold you have
- Reworked Midas Fruit relic: "Whenever you gain Gold, heal 1 HP for every 15
  Gold gained." -> "Upon pickup, add 1 Unripe Fruit to your Deck." The Unripe
  Fruit ripens into a Golden Fruit after 4 combats: "Heal 8 HP. Gain 25 Gold.
  Take an extra turn after this one. Exhaust."
- Buffed Double Dose card: Enchanted bonus increased from 1 -> 2 Weak
- Buffed Full Measure card: bonus damage per Enchanted card in Hand increased
  from 3 -> 4
- Reworked Inversion card: "Whenever you are healed, deal that much damage to ALL
  enemies 1 (2) time(s)." -> "Whenever you are healed, deal 50% (100%) of that
  much damage to ALL enemies."
- Removed the Gambit effect from Zenith card
- Masterwork card now glows gold when the card it Infuses will reach the
  7-Enchanted threshold
- Buffed Infuse: Attacks now gain 2 Toxic, increased from 1. Fuming and Exalted
  are unchanged at 1
- Renamed Drain Dry to Draining Strike
- Renamed Toxic Shard to Glowing Shard
- Reworked Auric Seal relic: "Cards you create are always Upgraded." -> "At the
  start of your turn, Infuse a random card in your Hand."
- Seep cards now glow green in Hand to signify something will happen if not
  played
- Ambergris potion now sells to the Merchant for 150g, up from 50g, to match how
  strong it is. It is the only potion whose price does not follow its rarity
- Updated flavor text for many relics
- Mod name in the mods list and mod-source tooltips changed from "The Alchemist
  (Alchemist)" -> "Alchemist"

### Fixed

- Citrinitas, Hemorrhage, and Ichor cards now show their full damage, including
  Strength, Vigor, and other damage effects
- Sublimate and Aureate card selection prompts no longer show a count of
  999999999
- Toxic enchantment now only applies Poison from the enchanted card's attack, not
  from its Poison triggers or HP loss
- Gold Leaf potion now shows its heal and Block total live in parentheses. Before,
  the value was missing or stale, because it was fixed when you got the potion
- Fixed the Orobas event never offering Archaic Tooth relic to the Alchemist; it
  now transcends Prime -> Aureate
- Fixed Dusty Tome relic being able to give Aureate, which now comes only from
  Archaic Tooth
- Fixed Winnow and Waste Not cards showing the raw text
  "card_selection.CHOOSE_CARD_HEADER" on their selection screen
- Fixed Inversion card damage being increased by Strength, Vigor, and Vulnerable;
  it is now always the stated percent of the heal

## [0.2.0] - 2026-07-19

### Changed

- Renamed many cards to match their mechanics and card types more closely
- Reworked Ferment: cards now keep their fermented potency after you play them,
  instead of one play using all of the potency
  - Nerfed Ferment rates to match:
    - Patient Strike: 100%(125%) -> 75%(100%)
    - Rolling Boil and Carapace: 75%(100%) -> 50%(75%)
    - Steep: 2 -> 1(2) Poison
    - Vintage: 2 -> 1 Regen
- Reworked Decant card: cost decreased from 1 -> 0, damage is now 5(7) and is no
  longer increased by Regen, and it adds a Distillate(+) into your Hand
- Changed Seep: now resolves before the Regen heal at the end of your turn, so
  Regen from Seep applies that turn
- Nerfed Sepsis card: Attack damage bonus against Poisoned enemies decreased from
  50% -> 25%(50%)
- Nerfed Trickle and Tinge cards: Seep decreased from 2 -> 1(2) Regen
- Nerfed Golden Touch card: cost is now 2
- Nerfed Bitter Draught card: now Exhausts
- Buffed Heavy Hand card: now increases the Poison you gain, in addition to the
  Poison you apply
- Buffed White Heat card: card draw increased from 1(2) -> 2(3)
- Buffed Inoculate card: Block increased from 6(9) -> 7(10)
- Buffed Poultice card: cost is now 0
- Buffed Last Resort card: HP loss without Gambit decreased from 5 -> 3
- Changed Resolve card: cost is now 3(2), and it grants 1 Strength for each 20 HP
  below your maximum HP at both ranks
- Changed Catalyze and Metabolism cards: now read "the first time you lose HP on
  each of your turns"
- Changed the Alchemist's attack, cast, and death sounds from the Ironclad's ->
  new sounds taken from the Silent and the Necrobinder
- Updated power icons for Golden Touch and Windfall

### Fixed

- Fixed power icons not loading for Bloom, Bottled Fury, Contagion, Drain Dry,
  Drip Feed, Fever Pitch, Golden Touch, Heavy Hand, and Twin Serpents
- Reflux and Suffuse cards now have power icons
- Hemorrhage card now shows its HP cost in red, next to the damage preview
- Fixed Ferment potency continuing into the next combat; it now resets at the
  start of each combat
- Fixed combat not ending when you kill the last enemy, which happened when your
  save contained a Timeline epoch from a mod you removed (unlock progress is
  unaffected)
- Fixed an event option that can kill you showing the placeholder text "Co-op
  survival line" in co-op

## [0.1.0] - 2026-07-15

The first pre-release. The mod is feature-complete and balanced. The public Steam
Workshop release waits for the character artwork.

### Added

- Added new playable character, the Alchemist
- Added 95 cards, including 4 multiplayer cards and 2 full-art Ancient rewards
- Added 9 relics, 3 potions, and 3 enchantments
- Added 4 class keywords: Gambit, Ferment, Seep, and Infuse
- Added 7-epoch timeline with progressive unlocks
- Added Ancient dialogue and character dialogue for the Alchemist, which connect
  to the game timeline
- Added an automated test suite for regressions and quality, which runs against
  the live game
- Added one-command build and publish workflow (scripts/dev.sh)
- Added offline lint that checks the sync between code, localization, and
  cards.csv
- Added build-time localization analyzer for the necessary power keys
- Added environment doctor and game-process controls for the test bridge
- Added release process: version policy, changelog, and a zip package for direct
  installation
