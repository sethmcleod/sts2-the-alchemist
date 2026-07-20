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

### Changed

- Increased Double Dose card Enchanted bonus 1 Weak -> 2 Weak
- Increased Full Measure card bonus damage per Enchanted card in Hand 3 -> 4
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
