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

- Auric Seal now Infuses a random card in your Hand at the start of your turn.
  Before, it Upgraded the cards that you create.
- Toxic Shard is now Glowing Shard. The effect does not change.
- Many relics have new flavor text.
- The mod name is now "Alchemist" in the mods list and in mod-source tooltips.
  Before, the tooltips showed "The Alchemist (Alchemist)".

## [0.2.0] - 2026-07-19

### Changed

- Many cards have new names. The new names match their mechanics and card types
  more closely.
- Sepsis now makes Poisoned enemies take 25% (50%) more Attack damage. Before,
  the increase was always 50%.
- Heavy Hand now increases the Poison that you gain. It also increases the Poison
  that you apply.
- Resolve now costs 3 (2). At both ranks, it grants 1 Strength for each 20 HP
  below your maximum HP.
- Golden Touch now costs 2.
- Bitter Draught now Exhausts.
- White Heat now draws 2 (3) cards, an increase from 1 (2).
- Inoculate now grants 7 (10) Block, an increase from 6 (9).
- Poultice now costs 0.
- Patient Strike (previously Culture) now counts as a Strike card.
- Decant has a new design. It costs 0. It deals 5 (7) damage, and Regen does not
  increase this damage. It also adds a Distillate (+) into your Hand.
- Without Gambit, Last Resort now makes you lose 3 HP, a decrease from 5.
- Ferment cards now keep their fermented potency after you play them. Before, one
  play used all of the potency.
- The Ferment rates are now lower to match this change:
  - Patient Strike: 100% -> 75% (125% -> 100%)
  - Rolling Boil and Carapace: 75% -> 50% (100% -> 75%)
  - Steep: 2 -> 1 (2) Poison
  - Vintage: 2 -> 1 Regen
- Trickle and Tinge now Seep 1 Regen instead of 2. When you upgrade them, they
  Seep 2.
- Seep now resolves before the Regen heal at the end of the turn. Thus, the Regen
  from Seep applies to that turn.
- Catalyze and Metabolism now read "the first time you lose HP on each of your
  turns".
- The Alchemist now has attack, cast, and death sounds of its own, instead of the
  sounds of the Ironclad. The new sounds come from the Silent and the
  Necrobinder.
- Golden Touch and Windfall have new power icons.

### Fixed

- The power icons now load again for Bloom, Bottled Fury, Contagion, Drain Dry,
  Drip Feed, Fever Pitch, Golden Touch, Heavy Hand, and Twin Serpents.
- Reflux and Suffuse now have power icons.
- Hemorrhage now shows its HP cost in red, next to the damage preview.
- Ferment potency no longer continues into the next combat. It resets at the
  start of each combat.
- Combat no longer stops when you kill the last enemy. This problem happened when
  your save contained a Timeline epoch from a mod that you removed. Your unlock
  progress does not change.
- In co-op, an event option that can kill you now shows a correct Alchemist line.
  Before, it showed the placeholder text "Co-op survival line".

## [0.1.0] - 2026-07-15

The first pre-release. The mod is feature-complete and balanced. The public Steam
Workshop release waits for the character artwork.

### Added
- New playable character: the Alchemist
- 95 cards, with 4 multiplayer cards and 2 full-art Ancient rewards
- 9 relics, 3 potions, and 3 enchantments
- 4 class keywords: Gambit, Ferment, Seep, and Infuse
- 7-epoch timeline with progressive unlocks
- Ancient dialogue and character dialogue for the Alchemist, which connect to the
  game timeline
- An automated test suite for regressions and quality. The suite runs against the
  live game.
- One-command build and publish workflow (scripts/dev.sh)
- Offline lint that checks the sync between code, localization, and cards.csv
- Build-time localization analyzer for the necessary power keys
- Environment doctor and game-process controls for the test bridge
- Release process: version policy, changelog, and a zip package for direct
  installation
