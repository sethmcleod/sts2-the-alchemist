# Changelog

All notable player-facing changes to The Alchemist are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see [RELEASING.md](RELEASING.md) for what a bump means for a mod). Each released
section below is also the update note posted to Steam Workshop for that version.

Sections follow Keep a Changelog: Added, Changed, Deprecated, Removed, Fixed,
Security.

## [Unreleased]

## [0.2.0] - 2026-07-19

### Changed

- Renamed many cards so names better match their mechanics and card types.
- Sepsis now makes Poisoned enemies take 25% (50%) more Attack damage, down from a
  flat 50%.
- Heavy Hand now boosts Poison you gain as well as Poison you apply.
- Resolve now costs 3 (2) and grants 1 Strength per 20 HP missing at both ranks.
- Golden Touch now costs 2.
- Bitter Draught now Exhausts.
- White Heat now draws 2 (3) cards, up from 1 (2).
- Inoculate now grants 7 (10) Block, up from 6 (9).
- Poultice now costs 0.
- Patient Strike (was Culture) now counts as a Strike card.
- Decant reworked: costs 0, deals 5 (7) damage with no Regen scaling, and adds a
  Distillate (+) into your Hand.
- Last Resort's non-Gambit HP loss lowered from 5 to 3.
- Ferment cards now keep their fermented potency when played instead of spending it
  on a single payoff.
- Ferment rates lowered to match: Patient Strike 100% -> 75% (125% -> 100%), Rolling
  Boil and Carapace 75% -> 50% (100% -> 75%), Steep 2 -> 1 (2) Poison, Vintage 2 -> 1
  Regen.
- Trickle and Tinge now Seep 1 Regen instead of 2, and 2 when upgraded.
- Seep now resolves before the end-of-turn Regen heal, so Regen it grants counts
  toward that turn.
- Catalyze and Metabolism now read "the first time you lose HP on each of your turns".
- The Alchemist now has its own attack, cast, and death sounds instead of the
  Ironclad's, drawn from the Silent's and Necrobinder's kits.
- Golden Touch and Windfall have new power icons.

### Fixed

- Power icons now load again for Bloom, Bottled Fury, Contagion, Drain Dry, Drip
  Feed, Fever Pitch, Golden Touch, Heavy Hand, and Twin Serpents.
- Reflux and Suffuse now have power icons.
- Hemorrhage now previews its HP cost in red, alongside the damage preview.
- Ferment potency no longer carries into the next combat; it resets at combat start.
- Combat no longer hangs on the killing blow when your save carries a Timeline epoch
  from a mod you have since uninstalled. Unlock progress is untouched.
- In co-op, an event option that would kill you now shows a real Alchemist line
  instead of the placeholder "Co-op survival line".

## [0.1.0] - 2026-07-15

First pre-release: feature-complete and balanced, pending character artwork
before the public Steam Workshop launch.

### Added
- New playable character: the Alchemist
- 95 cards, including 4 multiplayer cards and 2 full-art Ancient rewards
- 9 relics, 3 potions, and 3 enchantments
- 4 class keywords: Gambit, Ferment, Seep, and Infuse
- 7-epoch timeline with progressive unlocks
- Unique Ancient and character dialogue tied into the game timeline
- Automated regression and quality test suite run against the live game
- One-command build and publish workflow (scripts/dev.sh)
- Offline lint enforcing code / localization / cards.csv sync
- Build-time localization analyzer for required power keys
- Environment doctor and game-process controls for the test bridge
- Release process: versioning policy, changelog, and drop-in zip packaging
