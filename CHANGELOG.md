# Changelog

All notable player-facing changes to The Alchemist are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see [RELEASING.md](RELEASING.md) for what a bump means for a mod). Each released
section below is also the update note posted to Steam Workshop for that version.

Sections follow Keep a Changelog: Added, Changed, Deprecated, Removed, Fixed,
Security.

## [Unreleased]

### Changed
- The Alchemist now has its own attack, cast, and death sounds instead of playing
  the Ironclad's. Attacks and death draw on the Silent's kit, casts on the
  Necrobinder's, until the character has bespoke audio of its own.

### Fixed
- Combat no longer hangs on the killing blow when your save carries a Timeline
  epoch from a mod you have since uninstalled. The epoch is now omitted from the
  end-of-combat replay instead of aborting combat cleanup. Your unlock progress is
  untouched, so reinstalling the mod restores it.
- In co-op, choosing an event option that would kill you now shows a real line
  from the Alchemist instead of the placeholder text "Co-op survival line".

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
