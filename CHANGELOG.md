# Changelog

This document uses the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
format. This project also follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see [RELEASING.md](RELEASING.md) to know what each version increase means for a
mod).

## [Unreleased]

## [0.11.1] - 2026-08-20

### Changed

- Buffed Mash: damage 4 (6) -> 5 (8), so the attack body stays ahead of Thicken's Block
- Changed Antitoxin to no longer have an upper limit
- Changed the Mix maker wording to the base game's: "Choose a Mix to add into your Hand."
- Nerfed Apothecary: cost 2 (1) -> 3 (2)
- Nerfed Bursting Mix: damage 8 -> 6
- Nerfed Fuming Mix: it now also grants 1 Poison
- Nerfed Sturdy Mix: Block 6 -> 5
- Reworked Double Batch into Fresh Batch: "Choose a Mix to add into your Hand. Draw 1 card."
- Reworked Fortified (Infused Powers): playing the Power now grants "Whenever you gain Poison, gain 1 Antitoxin" instead of raising the Antitoxin limit
- Reworked Snake Tail relic: "The first time your Antitoxin reaches 0 each combat, gain 8 Antitoxin."
- Reworked Tolerance: "At the start of your turn, gain Antitoxin equal to your Poison." Cost 1 -> 3, it gains Ethereal, and Tolerance+ loses Ethereal instead of growing

## [0.11.0] - 2026-08-20

### Added

- Added Adapt card: "Gain 7 (10) Block. If your Antitoxin absorbed damage this turn, gain 1 Energy."
- Added analytics: which Mix players choose, and how much Poison damage Antitoxin absorbed versus how much reached HP. The dashboard shows both, and every dot on the card chart now carries its name
- Added Apothecary card: "At the start of your turn, choose a Mix to create." Cost 2 (1)
- Added Blend card: "Gain 5 (8) Block. Transform a card in your Discard Pile into a Mix of your choice."
- Added Bloom card: "Ferment ALL cards in your Hand 3 (4) additional times. Exhaust."
- Added Brine card: "Retain. Ferment. Gain 5 (7) Block. Gain 3 additional Block and 1 Poison for each turn fermented."
- Added Condense card: "Transform 1 (2) cards in your Draw Pile into Mixes of your choice. Put them into your Hand."
- Added Dose card to the starting deck: "Gain 2 Block and 2 Poison. Gain 3 (6) Antitoxin." It replaces Antidote
- Added Double Batch card: "Choose 2 (3) Mixes to create."
- Added Fizz card: "This turn, whenever you gain Poison, deal 3 (4) damage to ALL enemies."
- Added Forked Tongue card: "Deal 3 (4) damage twice. Each hit deals additional damage equal to your Poison."
- Added Grand Batch card: "Create one of each Mix. Exhaust." Cost 1 (0)
- Added Ichor card: "At the start of your turn, gain 2 Poison. Your Attacks deal additional damage equal to your Poison."
- Added Inure card: "Increase your Antitoxin limit by 3 (4) for the rest of combat. Gain 3 (4) Antitoxin."
- Added Lick card: "Deal 3 (5) damage. Deals additional damage equal to your Poison."
- Added Mash card: "Deal 4 (6) damage. Choose a Mix to create."
- Added Miasma card: "Whenever you gain Poison, apply that much Poison to ALL enemies."
- Added Numb card: "Gain 14 (18) Block. Gain 3 Poison."
- Added Osmosis card: "At the start of your turn, gain 3 (4) Antitoxin."
- Added Overdose card: "Gain 5 Poison. Gain 12 (16) Antitoxin. Exhaust."
- Added Reagent card: "This turn, your Attacks count your Poison twice."
- Added Retch card: "Apply Poison equal to your Poison. Exhaust."
- Added Simmer card: "At the start of your turn, gain 1 (2) Poison."
- Added Spew card: "Deal 7 (10) damage. Apply Poison equal to your Poison."
- Added Starter Culture potion (Brew only): "Trigger ALL Ferment cards in your Hand 3 times."
- Added Steep card: "Ferment a card in your Hand 2 (3) additional times. Draw 1 card."
- Added Stir card: "Choose a Mix to create." Cost 1 (0)
- Added the Mix family: choosing a Mix shows all four and you pick one. Bursting Mix "Deal 8 damage.", Fuming Mix "Apply 1 Weak and 1 Vulnerable.", Sturdy Mix "Gain 6 Block.", Zesty Mix "Draw 1 card. Gain 3 Antitoxin." All cost 0 and Exhaust
- Added Thicken card: "Gain 4 (7) Block. Choose a Mix to create."

### Changed

- Buffed Brine: base Block 5 (7) -> 6 (8)
- Buffed Caustic Strike: Antitoxin 2 (3) -> 3 (5)
- Buffed Froth: base damage 4 (6) -> 5 (7)
- Buffed Nightcap: Antitoxin 5 (6) -> 7 (9)
- Buffed Patient Strike: base damage 6 -> 7 (9); damage per turn fermented 6 (9) -> 6 (8)
- Buffed Poultice: Block 8 (11) -> 9 (12)
- Buffed Quicklime: damage 5 (7) -> 6 (8), and the bonus now triggers if you have Poison instead of if the enemy is Poisoned
- Buffed Resolve: self Poison 5 -> 4
- Buffed Rolling Boil: base damage 6 -> 7 per hit
- Buffed Siphon: if the drawn card is already Enchanted, it now grants 3 Antitoxin instead
- Buffed Slow Burn: base damage 5 -> 6 (8)
- Buffed Solvent: it now also removes ALL Artifact from the enemy
- Changed Antitoxin: it no longer becomes Block once you are at the limit; a grant past the limit is lost
- Changed Antitoxin: it no longer cures the Poison it absorbs. Your dose stays in place and powers your attacks; Antitoxin pays the ticks
- Changed Brew: it now offers a choice of 3 of the Brew-only Potions instead of one random one
- Changed Callus: cost 1 -> 2 (1), Strength per trigger 1 (2) -> 1
- Changed Effervesce: it now adds 2 Zesty Mixes to another player's Hand
- Changed Ferment: a played Ferment card now goes to your Discard Pile like any other card, so it can be drawn and fermented again. It still adds a Residue
- Changed Lash Out: the extra hit now triggers if you have Poison instead of if the enemy is Poisoned
- Changed the Antitoxin limit: 12 -> 20
- Changed the Brew run badge into the Mixes badge: it now counts the Mixes you created, at 10, 20 and 30 for Bronze, Silver and Gold
- Changed the Ferment wording: cards now read "Trigger ALL Ferment cards in your Hand" instead of "Ferment ALL cards in your Hand"
- Changed the fifth Epoch: it now unlocks All At Once, White Heat and Spatter
- Changed the Mix hover tip: one tip describes all four Mixes instead of four stacked card previews
- Changed the Potion Sale badge tiers: 3, 6 and 9 sales -> 2, 4 and 6, since a Brew now yields at most one sellable Potion
- Changed the sixth Epoch: it now unlocks Ichor, Miasma and Mercurial Form
- Changed Weathered Kit and Gilded Kit: they no longer add a Distillate to your Hand at the start of combat
- Changed Weathered Kit: "At the start of each combat, gain 8 Antitoxin and 1 Poison."
- Nerfed Gulp: it now Exhausts
- Nerfed Puncture: Vulnerable 2 (3) -> 1 (2)
- Nerfed Swill: cost 0 -> 1
- Renamed Dregs to Residue
- Renamed Purge to Double Dose, Pays Off to Smelling Salts, Next Up to Anoint, Fresh Coat to Alembic, and Elixir to Panacea
- Reworked All At Once: "Deal damage equal to your Poison X (+1) times. Exhaust." It now hits one enemy and no longer removes your Poison
- Reworked Auric Seal relic: "The first card you draw each turn is Infused, or grants 2 Antitoxin if it is already Enchanted."
- Reworked Congeal: "Gain 6 (9) Block. If you have Poison, draw 1 card."
- Reworked Corrode: "Apply Poison equal to your Poison and 1 (2) Weak to ALL enemies." It no longer removes your Poison
- Reworked Croak: "Deal 18 (24) damage. Apply twice your Poison. Exhaust."
- Reworked Deep Cut: "Deal 10 (13) damage. Deals additional damage equal to twice your Poison."
- Reworked Double Dose: "Deal 4 (6) damage. Deals additional damage equal to twice your Poison." It no longer removes your Poison
- Reworked Eureka: it now transforms a card in your Hand into a Mix of your choice instead of a Distillate
- Reworked Everflowing Chalice relic: "At the start of each combat, choose a Mix to create." It no longer procures a Potion
- Reworked Fallout: "Deal 4 (6) damage to ALL enemies. Deals additional damage equal to your Poison."
- Reworked Flare Up: "Deal 8 (11) damage. If you have 3 or more Poison, gain 1 Energy and draw 1 card."
- Reworked Gilded Kit: "You can Brew at Rest Sites and gain 1 extra potion slot."
- Reworked Gold Leaf potion: "Gain 1 Block and 1 Antitoxin for every 15 Gold you have." It no longer heals
- Reworked Jab: "Deal 6 (9) damage. Deals additional damage equal to your Poison." Cost 0 -> 1, and it no longer applies Poison to the enemy
- Reworked Laced enchantment: "This Attack deals additional damage equal to your Poison."
- Reworked Mercurial Form: "At the start of your turn, gain 1 Poison and gain Strength equal to your Poison this turn." It no longer removes your Poison or damages you
- Reworked Panacea: "At the start of your turn, gain 2 Poison and 3 (4) Antitoxin." It no longer procures Potions
- Reworked Quaff: "Gain 3 (4) Antitoxin. Gain Antitoxin equal to your Poison."
- Reworked Quench: "Increase your Antitoxin limit by 3 (4) for the rest of combat. Gain 6 (9) Block and 3 Antitoxin."
- Reworked Refine: "Infuse any number of cards in your Hand." with a card selection
- Reworked Refined Extract potion: "Choose 2 Mixes to create."
- Reworked Rolling Boil: "Retain. Ferment (0). Deal 5 (7) damage to ALL enemies. Deals 4 additional damage for each turn fermented."
- Reworked Spatter: "Deal 6 (9) damage. Lose 3 (4) Poison. Apply that much Poison." It is now Uncommon
- Reworked Sweat It Out: "Retain. Ferment. Gain 2 Poison, plus 2 (3) for each turn fermented. Apply Poison equal to your Poison to ALL enemies." It no longer removes your Poison
- Reworked Toughen: "Gain 5 (7) Block. Gain 2 additional Block for each Poison you have." It no longer removes your Poison
- Reworked Toxin Skin: "Whenever you take unblocked attack damage, transfer 2 (3) Poison to the attacker."
- Reworked Vitrify: "Gain 8 (12) Antitoxin."
- Reworked White Heat: "Deal damage equal to 3 (4) times your Poison to ALL enemies. Exhaust."
- Reworked Wormwood: "Deal 3 (4) damage to ALL enemies. Deals additional damage equal to your Poison. Gain 2 (3) Antitoxin."

### Removed

- Removed Antidote card, replaced by Dose in the starting deck
- Removed Chain Reaction, Recoil, Rot, Weak Spot and Boil Down cards, replaced by the five Rare cards above
- Removed Decant, Deep Breath, Enrich, Fester, Grudge, Heavy Hand, Hone, Kickback, Masterwork, Melt Down, Swamp Gas and Winnow cards, replaced by the thirteen Uncommon cards above
- Removed Dispense card
- Removed Percolate card, replaced by Blend
- Removed Taint card, replaced by Forked Tongue
- Removed Distillate token, replaced by Mix tokens
- Removed Waste Not, Reckless Swing, Inoculate, Fast Acting and Sublimate cards, replaced by the Mix cards above

### Fixed

- Fixed a replayed Ferment card losing its fermentation on the extra plays. The stack now applies to every play and resets after the last
- Fixed Callus and Second Skin describing their trigger as taking Poison damage; they trigger whenever your Poison triggers, including ticks Antitoxin absorbs
- Fixed Harden not paying Block for Poison an enemy applies to you
- Fixed potions that cannot be sold shaking and showing the gold icon when entering a shop
- Fixed Quicklime's card face not showing its total damage while you have Poison
- Fixed Smelling Salts, Reagent, Immunize and Reflux descriptions understating or overstating their triggers
- Fixed Spit keeping your Poison when the hit killed the target
- Fixed the Alchemist's blinking and staff orb shine animations continuing to play after death
- Fixed the Alchemist's relic icons missing their character-colored outline in the Relic Collection
- Fixed Toxin Skin's card face not rendering its Poison number
- Fixed Grand Batch and Effervesce not counting their Mixes for the Mixes badge and analytics
- Fixed Effervesce's upgrade doing nothing; Effervesce+ now adds 3 Zesty Mixes
- Fixed Auric Seal carrying its once-per-turn state from one combat into the next
- Fixed Auric Seal and Siphon describing the Antitoxin fallback as only for Enchanted cards; it applies whenever the drawn card cannot be Infused
- Fixed the Alchemist's relics missing the purple outline that character relics show in the Relic Collection
- Fixed the capacity cards saying "for the rest of combat"; they now say "this combat" like the base game
- Fixed the character name overlapping the Antitoxin bar when hovering; the name now sits below the bar, and the Antitoxin number fades with the HP number
- Fixed the Infuse keyword tip naming the Power enchantment Potent; it is Fortified
- Fixed White Heat and Gold Leaf showing their total on the same line as the count they read

## [0.10.1] - 2026-08-17

### Added

- Added a Steam Workshop self-update check on startup: when Steam has a stale copy of the mod, it re-downloads the latest and asks you to restart

## [0.10.0] - 2026-08-17

### Added

- Added anonymous run analytics, sent only when the game's own Upload Data setting is on. Includes a Share Run Analytics toggle in the mod config to opt out

## [0.9.8] - 2026-08-17

### Added

- Added Quaff card: "Gain 2 (3) Antitoxin. Draw 1 (2) cards. Draw 1 additional card for every 4 Antitoxin you have."
- Added Ripen card: "At the start of your turn, Ferment ALL cards in your Hand 1 additional time."

### Changed

- Improved VFX and animations used for many cards
- Renamed One For Me card to Nightcap
- Reworked Fresh Coat card: "Whenever you Enchant a card, increase your Antitoxin limit by 1 (2) and gain 1 (2) Antitoxin."
- Reworked the Power Infusion: Infused Powers now increase your Antitoxin limit by 2 when played instead of giving 1 Strength

### Removed

- Removed Blood Rush card, replaced by Quaff
- Removed Share The Pain card, replaced by Ripen

## [0.9.7] - 2026-08-17

### Added

- Added Brace card: "Gain 5 (8) Block. If you have Poison, gain 1 Energy."

### Changed

- Buffed Swill card: it now also draws a card, so it is never blank when you have no Ferment cards
- Buffed Vitrify card: 3 (4) Block twice -> 4 (5) Block twice
- Reworked Waste Not card: "Gain 4 (7) Block. Exhaust all Dregs in your Hand. Gain 2 Antitoxin for each."
- Nerfed Eureka card: it now Transforms 1 card instead of 2
- Nerfed Melt Down card: 0 Energy -> 1 Energy

### Removed

- Removed Nettle card, replaced by Brace

## [0.9.6] - 2026-08-17

### Added

- Added Caustic Strike card: "Deal 6 (9) damage. Gain 2 (3) Antitoxin." It counts as a Strike
- Added Dregs card: "At the end of your turn, if this is in your Hand, gain 2 Poison. Exhaust."

### Changed

- Buffed Deep Cut card: 1 Energy, 7 (10) damage, draw 1 -> 2 Energy, 13 (16) damage, draw 1 (2). It no longer applies Vulnerable
- Buffed Gilded Kit relic: it now also adds a Distillate+ into your Hand at the start of each combat
- Buffed Harden card: it gives Block when you gain or apply Poison again, not only when you gain it
- Buffed Jab card: self Poison 1 -> 2, so Antitoxin has something to absorb in the early game
- Buffed One For Me card: 1 Energy, 8 (11) damage, 3 (4) Antitoxin -> 2 Energy, 14 (18) damage, 5 (6) Antitoxin
- Buffed Puncture card: Vulnerable 1 (2) -> 2 (3)
- Buffed Weathered Kit relic: it now also adds a Distillate into your Hand at the start of each combat
- Changed Brew: it gives 1 Potion for both Kits again, instead of 2 with the Gilded Kit
- Changed Ferment: playing a Ferment card now adds a Dregs to your Discard Pile instead of a Toxic
- Reworked Antidote: "Gain 6 (9) Block. Gain 2 (3) Antitoxin." It is no longer a Ferment card
- Reworked Poultice: "Gain 8 (11) Block. Gain 2 Poison." It is no longer a Ferment card

### Removed

- Removed Rummage card, replaced by Caustic Strike

### Fixed

- Fixed the Callus power sharing an icon with Weak Spot

## [0.9.5] - 2026-08-16

### Fixed

- Fixed Fester never wearing off, so its extra Poison triggers kept applying every turn instead of
  only the enemy's next turn

## [0.9.4] - 2026-08-16

### Fixed

- Fixed placeholder icons for Fester, Grudge, Heavy Hand, Share The Pain, Toxin Skin and Winnow powers
- Fixed Sampler not counting itself, so it gave 1 less Energy and drew 1 less card than it promised

## [0.9.3] - 2026-08-16

### Fixed

- Fixed the Compendium showing no cards for any character on Windows, and the Alchemist's icon not
  loading on the character select screen

## [0.9.2] - 2026-08-16

### Added

- Added Percolate card: "Add 2 (3) Distillates into your Hand. Exhaust."
- Added Quench card: "Gain 3 (4) Antitoxin. Gain 4 (6) Block, plus 2 Block for each Antitoxin you have."
- Added Sublimate card: "At the start of your turn, deal damage equal to your Antitoxin to the enemy with the most HP."
- Added vfx for Marsh Tonic potion

### Changed

- Buffed Heavy Dose: 26 (32) damage -> 35 (45) damage, and it now gains 3 Poison
- Buffed Puff Up: 10 Block -> 10 (13) Block
- Buffed Spatter: its splash now also applies 1 (2) Poison to ALL other enemies
- Buffed Tempered: 30 (35) Block -> 30 (40) Block
- Changed Antitoxin: it Block once you are at your max, and that Block gain is increased by Dexterity
- Nerfed Harden: it now only gives Block when you gain Poison, not when you apply it to an enemy
- Reworked Quicklime: "Deal 5 (7) damage. Deal 4 (6) additional damage if the enemy is Poisoned."
- Reworked Toughen: "Lose all Poison. Gain 4 (6) Block. Gain 2 additional Block for each Poison lost."

### Fixed

- Fixed Antitoxin hover tip not updating when your max was increased

### Removed

- Removed Contagion card, replaced by Sublimate
- Removed Flush card, replaced by Percolate

## [0.9.1] - 2026-08-16

### Changed

- Changed Laced enchantment: "Whenever this Attack deals unblocked damage, apply 2 Poison."
- Updated Brew rest option description to clarify that the potions can be sold to the Merchant

### Fixed

- Fixed Slow Burn getting stuck on-screen when played

## [0.9.0] - 2026-08-16

### Added

- Added Anodyne potion: "The next time you would take unblocked attack damage, prevent it. Gain 1 Poison for every 4 damage prevented."
- Added Decoction potion: "Draw 3 cards. They cost 0 this turn."
- Added Sampler potion: "Gain 1 Energy and draw 1 card for each Potion you have, including this one."
- Added Solvent potion: "Remove all Block from the enemy. Apply 3 Weak to it."

### Changed

- Added, removed, reworked and renamed many cards to be more focused and balanced
- Buffed Gilded Kit relic: Antitoxin at the start of combat 6 -> 8, and Brew now gives 2 Potions
- Buffed Weathered Kit relic: Antitoxin at the start of combat 3 -> 4
- Changed Antitoxin limit: 9 -> 12
- Changed Antitoxin: it no longer becomes Block once you are at the limit
- Changed Antitoxin: it now cures 1 Poison for each point of damage it absorbs, instead of only preventing the damage
- Changed Brew: it now offers only the 6 Potions that can be obtained no other way
- Changed Dosed enchantment: Antitoxin 1 -> 2
- Changed Ferment: it no longer has a peak and no longer spoils. A Ferment card now becomes a Toxic in your Discard Pile when you play it
- Changed Infuse: enchantments no longer stack, matching every other enchantment in the game
- Changed Laced enchantment: "This Attack deals additional damage equal to your Poison." It no longer applies Poison to you
- Changed Potion sale prices: a shop discount such as Membership Card or The Courier no longer lowers what the Merchant pays you
- Changed Potion selling: only brewed Potions can be sold, and they now sell for their full value
- Changed starting Gold: 75 -> 99
- Changed starting HP: 69 -> 75
- Reworked Quicksilver Draught potion: "Trigger Poison on an enemy once for each Poison it has."
- Reworked Snake Tail relic: "Your Antitoxin limit is increased by 6." It no longer revives you

### Removed

- Removed Damage Forecast setting from mod config
- Removed Soporific potion, replaced by Anodyne
- Removed the Potion Sell Price and Brew-Only Chance settings. Both are now fixed values so that future tuning reaches every player

### Fixed

- Fixed being locked out of the main menu after unlocking an Alchemist epoch in a run that did not finish, such as leaving a Multiplayer game part way through
- Fixed Second Skin relic paying nothing when Antitoxin absorbed the whole tick of Poison damage, which did not match its description or how Callus and Contagion behave
- Fixed several Power descriptions understating their own values
- Fixed the Infuse card picker offering cards it could not Enchant

## [0.8.0] - 2026-08-14

### Added

- Added Antitoxin: it absorbs damage you would take from Poison, up to a maximum of 9
- Added new animations to the character model: attack, heavy attack and cast
- Added Spare Dose relic: "Whenever you gain Poison, gain 1 Antitoxin."

### Changed

- Added, removed, reworked and renamed many cards to be more aligned with the base game
- Changed Snake Tail into a Rare relic
- Changed the default Potion sale price: 100% -> 50% of the shop value, and its range is now 10% to 90%
- Reworked Fuming enchantment into Dosed: now gains 1 Antitoxin when played
- Reworked Laced enchantment: now applies 1 Poison and gains 1 Poison when played
- Reworked Marsh Tonic potion: "Gain 6 Antitoxin."
- Reworked Glowing Shard relic: "Whenever you gain Poison, apply 1 Poison to a random enemy."
- Reworked Viriditas relic and renamed it Second Skin: "Whenever you take Poison damage, gain 2 Block."
- Updated map drawing ink color to be purple

### Removed

- Removed Gambit keyword
- Removed Reaction keyword
- Removed Unstable keyword and the potion archetype. Elixir is now the only card that makes potions
- Removed Quintessence relic

### Fixed

- Fixed Gilded Kit not being in the event pool, like the other characters' upgraded starter relics
- Fixed Orobas dialogue that named the removed Nigredo and Albedo cards
- Fixed Masterwork not Exhausting on the play that meets its condition
- Fixed Golden Touch, Weak Spot, Grudge and Rot affecting other players in Multiplayer
- Fixed Snake Tail preventing deaths that Poison did not cause
- Fixed Gold Leaf potion being able to appear from in-combat potion generation, like Alchemize
- Fixed the Merchant buying back Potions from the Cauldron relic bought at that shop
- Fixed missing VFX for Soporific potion

## [0.7.6] - 2026-08-13

### Changed

- Bumped baselib dependency to latest version (v3.4.5) and deleted redundant stats page patch

### Fixed

- Fixed Flare Up causing a State Divergence in Multiplayer: its Poison trigger now lands as a real Poison tick, and no longer targets an enemy the attack already killed

## [0.7.5] - 2026-08-13

### Fixed

- Fixed the Alchemist having no hitbox at Rest Sites, which stopped allies from targeting them with Mend in Multiplayer

## [0.7.4] - 2026-08-12

### Changed

- Renamed the Coveted Potion tip to High Quality
- Changed the Potion Sell Price setting: it now runs from 10% to 100% in steps of 10%

### Fixed

- Fixed unlimited Gold at the Merchant: he no longer buys back a Potion you bought from him at that same shop, and the sale price now follows any shop discount you have, such as The Courier

## [0.7.3] - 2026-08-12

### Changed

- Reworked the Reaction (Exhaust) keyword: it now triggers if any card Exhausted this turn, instead of only the card you played directly before this one
- Buffed Infuse keyword: Attacks now gain 2 Laced, increased from 1
- Changed Unstable Compound card: it now has Ethereal instead of Exhaust
- Change color of the word Merchant to be gold outside of epoch descriptions

### Fixed

- Fixed the Alchemist not always being targetable by the Mend rest site option in Multiplayer

## [0.7.2] - 2026-08-11

### Added

- Added Macerate card: "Deal 5 (7) damage. Deals additional damage equal to your Poison."

### Changed

- Changed Sinter into a Common card
- Changed Patient Strike into an Uncommon card
- Changed Carapace into an Uncommon card. Retain cards no longer appear in the Common pool

### Removed

- Removed Aggravate card

### Fixed

- Fixed Eureka+ transforming a card into Distillate instead of Distillate+

## [0.7.1] - 2026-08-11

### Added

- Added Share The Pain card: "Whenever you gain Poison, deal that much damage to ALL enemies."

### Changed

- Changed Reagent card and the Reactive power: both now read "the next card you play with a Reaction"
- Changed Quintessence relic: it now reads "the first card you play with a Reaction"
- Changed the Ferment keyword tip: it now says that playing the card resets it's fermentation
- Changed the Reaction keyword tips to the base game's "If ..." phrasing, and they now say the matching card must be played in the same turn
- Changed Heavy Hand card and power: both now read "apply an additional N", matching the base game's wording for extra debuff stacks
- Changed Patient Strike into a Common card, and its damage for each turn fermented decreased from 6 (9) -> 4 (6)
- Changed Froth into an Uncommon card
- Nerfed Resolve card: cost increased from 1 -> 2
- Nerfed Percolate card: cards drawn decreased from 3 (4) -> 2 (3)
- Nerfed Quicklime card: it no longer gains Block unconditionally, and its Reaction (Skill) Block increased from 3 -> 5 (7)
- Reworked Venom Trance into a Rare card: "Gain 30 Poison. Take an extra turn after this one. Exhaust."
- Buffed Overflow card: it now gains 1 Regen first, so it is never a dead draw
- Nerfed Lifeblood card: it no longer gains 2 Regen, and only scales off the Regen you already have
- Nerfed Congeal card: Gambit Regen decreased from 3 (4) -> 2 (3)
- Nerfed Poultice card: cost increased from 0 -> 1 (0), and its Regen no longer increases on upgrade
- Reworked Overdose card: its Reaction (Exhaust) now draws 1 card instead of gaining 2 (3) Regen
- Reworked Cauterize card: its Reaction (Attack) now deals 3 additional damage instead of gaining 1 Regen
- Added Catalysis card: "The first time you trigger a Reaction each turn, draw 1 (2) cards."
- Added Corrosive card: "Whenever you apply Weak or Vulnerable to an enemy, apply 1 (2) Poison to it."
- Nerfed Infuse keyword: Attacks now gain 1 Laced, decreased from 2
- Changed Prime card: its Reaction (Attack) now grants 2 additional Block instead of a second Infuse
- Nerfed Refine card: cost increased from 0 -> 1
- Nerfed Puncture card: Gambit Weak decreased from 2 -> 1
- Nerfed Vivisect card: cards drawn decreased from 2 -> 1, and it no longer applies Weak when Enchanted
- Changed the Ferment keyword: every Ferment card now peaks at 3 turns. Carapace and Sweat It Out decreased from 5 -> 3
- Reworked Deep Breath into Anneal, a Common card: "Gain 6 Block. Gain 2 (1) Poison. Exhaust." It no longer Retains
- Changed Sinter into an Uncommon card
- Fixed the Lifeblood and Overflow hover tips: they now match what each card does after this release's Regen changes
- Fixed Catalysis not drawing when its Reaction card resolved another card play inside its own
- Reworked Fuming enchantment: playing the card now applies 1 Weak and 1 Vulnerable to a random enemy and gives you 1 Poison, instead of adding a Foul Vapor into your Hand
- Reworked Vintage card: "Retain. Ferment (0/3). Gain 1 Energy and draw 1 card for each turn fermented. Exhaust." Cost decreased from 2 -> 1(0)

### Removed

- Removed Catalyze card
- Removed Drip Feed card
- Removed Bramble card
- Removed Foul Vapor card

## [0.7.0] - 2026-08-11

### Added

- Translated all mod text into the other 14 languages the game supports: Chinese (Simplified), Chinese (Traditional), Russian, German, French, Spanish (Spain), Spanish (Latin America), Italian, Japanese, Korean, Polish, Portuguese (Brazil), Thai and Turkish. Keywords follow the base game's own wording

### Changed

- Nerfed Weathered Kit relic: it no longer heals 3 HP whenever you use a Potion
- Nerfed Gilded Kit relic: it no longer heals 5 HP whenever you use a Potion
- Nerfed Decant card: cost increased from 0 -> 1, and damage increased from 3 (5) -> 8 (11)
- Nerfed Quench card: cost increased from 0 -> 1
- Nerfed Precipitate card: cost is now 3 (2), and its upgrade no longer grants 1 additional Block

## [0.6.9] - 2026-08-11

### Fixed

- Fixed the mod failing to load on the public version of the game. The Timeline feature now turns itself off there instead, and every card, relic and potion is available from the start

## [0.6.8] - 2026-08-11

### Changed

- Changed Venom Trance, Golden Fruit and Quicksilver Draught to grant a visible Extra Turn power instead of the base game's hidden counter

### Fixed

- Fixed a stale Infuse reference at the end of combat that could stop Smith and other upgrades from working for the rest of a run

## [0.6.7] - 2026-08-11

### Changed

- Improved description for Echo Strike card

### Fixed

- Fixed an issue where Brewing in multiplayer could make rest options invisible
  for allies
- Fixed an issue where Mend rest site option could not be targeted if Alchemist
  was in the party

## [0.6.6] - 2026-08-10

### Added

- Added Echo Strike card: "Deal 6 (8) damage. If this card is Enchanted, hits an additional time."

### Changed

- Changed Nigredo into a Rare card and removed it from the starting deck. Reagent is now a starting card in its place
- Reworked Prime into a Skill: "Gain 4 (7) Block. Infuse a card in your Hand. Reaction (Attack): Infuse an additional card."

### Removed

- Removed Zenith card

### Fixed

- Fixed the campfire options turning invisible for other players in Multiplayer after the Alchemist used a campfire action
- Fixed Mend not being able to target the Alchemist in Multiplayer

## [0.6.5] - 2026-08-10

### Added

- Added a Brew hover tip to the Weathered Kit and Gilded Kit relics

### Changed

- Nerfed Brew Rest Site option so that it no longer removes a card from your Deck
- Changed the Infuse hover tips to also preview the Foul Vapor token that Fuming creates
- Updated Potion sale run summary badge placeholder art

## [0.6.4] - 2026-08-09

### Added

- Added the Alchemist character art to the character select screen, with a subtle
  breathing idle. The background swirls sit behind the character, and the pulsing
  lights and rising motes stay in front

### Changed

- Darkened the character select background gradient so the character art stands out

## [0.6.3] - 2026-08-09

- Added the Alchemist hands used in multiplayer, replacing the Ironclad placeholders

## [0.6.2] - 2026-08-09

### Added

- Added the Alchemist character model to combat, replacing the Ironclad placeholder. It idles, blinks and reacts when hit
- Added the Alchemist character model to the Rest Site and the Merchant, replacing the Ironclad placeholders
- Added the Sell option to Foul Potion, paying the same Gold as throwing it at the Merchant

### Changed

- Changed the Coveted potion tip: it now shows anywhere in a run instead of only at the Merchant

### Fixed

- Fixed the Brew Rest Site option showing a blank icon
- Fixed the Unstable Compound power tooltip showing placeholder text instead of the turn and the damage number

## [0.6.1] - 2026-08-09

### Added

- Added run summary badges earned by Brewing at 3, 6 or 9 Rest Sites in a run
- Added run summary badges earned by selling 3, 6 or 9 Potions to the Merchant in a run

### Changed

- Renamed Aqua Vitae relic to Quintessence and reworked it: "Whenever you use a Potion, gain 1 Max HP." -> "The first card you play each turn triggers its Reaction."
- Renamed Flux Stone relic to Viriditas and reworked it: "Whenever a card is created in combat, draw 1 card." -> "At the start of each combat, gain 3 Regen."
- Reworked Gilded Kit relic: using a Potion now grants 1 Max HP instead of 1 Strength, and its heal decreased from 6 -> 5
- Buffed Gambit keyword: it now activates at 50% or less HP, increased from 33%
- Buffed Fever Pitch card: cost decreased from 2(1) -> 1(0)
- Nerfed Unstable Compound card: upgraded damage decreased from 20 -> 18
- Nerfed Hone card: cost increased from 1 -> 2(1), and its upgrade no longer grants 1 Strength
- Changed Citrinitas card: cost is now 1(0), and its upgrade no longer grants 2 additional damage
- Nerfed Deep Breath card: it now gains Block 1 and Exhausts; its upgrade removes the Exhaust
- Changed the Nigredo chain hover tips: each one now previews the other three in order

### Fixed

- Fixed the Regen from upgraded Albedo not being applied if you had no Poison
- Fixed an issue where the Drink/Throw button was not being rendered for Potions
  in the Shop
- Fixed the Block from Fresh Batch and from Quicklime's Reaction not counting for a Reaction (Block)

## [0.6.0] - 2026-08-02

### Added

- Added Reaction keyword: "When you play this card directly after a card that matches, it has a Reaction effect."
- Added Reagent card: "Deal 4 (6) damage. The next card you play this turn triggers its Reaction." Replaces Trickle
- Added Backdraft card: "Deal 8 (11) damage. Reaction (Attack): Deal 8 (11) damage again." Replaces Unstable Compound
- Added Reactive power: "The next card you play with a Reaction will trigger it."
- Added Etch card: "Deal 14 (18) damage. Ignore Block." Replaces Grind Down
- Added Slag card: "Whenever you Exhaust a card, gain 1 (2) Block." Replaces Metabolism

### Changed

- Nerfed Gambit keyword: it now activates at 33% or less HP, decreased from 50%
- Reworked Ferment keyword: a card now spoils into a Toxic once it passes its peak, and its face reads "Ferment (0/3)" for turns fermented over that peak
  - Patient Strike and Rolling Boil peak at 3 turns, Froth and Vintage at 4, Carapace and Sweat It Out at 5
  - Playing a Ferment card now resets its turns fermented, so each draw starts a fresh ramp
- Buffed Sweat It Out card: Poison per turn fermented increased from 1 -> 2
- Nerfed Fumigate card: extra hits changed from 1 per card in your Exhaust Pile -> 1 per 2 cards
- Buffed Prime card: Gambit Block increased from 4(6) -> 7(10)
- Buffed Puncture card: Gambit Weak increased from 1 -> 2
- Buffed Congeal card: Gambit Regen increased from 2(3) -> 3(4)
- Reworked Fresh Batch card: "Procure a random potion. Gambit: Gain 10 (14) Block. Exhaust."
- Reworked Lash Out card: "Deal 6 (8) damage 3 times. Gambit: Gain 1 Regen for each hit. Reaction (Power): Hits an additional time."
- Changed Fever Pitch and Suffuse cards: they now read "While Gambit is active" instead of naming an HP percentage
- Reworked Quicklime card: "Deal 7 (9) damage. Gain 5 (7) Block. Reaction (Skill): Gain 3 Block."
- Reworked Cauterize card: "Deal 6 (9) damage. Gain 1 Regen. Reaction (Attack): Gain 1 additional Regen."
- Reworked Overdose card: "Lose 4 HP. Deal 15 (20) damage. Reaction (Exhaust): Gain 2 (3) Regen."
- Reworked Draining Strike card: "Deal 14 (18) damage. The enemy loses 6 Strength this turn. Reaction (Attack): Deal 5 (7) additional damage."
- Reworked Transmute card: "Gain Strength this turn equal to your Poison. Reaction (Exhaust): Procure a random Common (Uncommon) potion."
- Reworked Corrode card: "Apply 6 Poison and 1 (2) Weak to ALL enemies. Reaction (Skill): Apply 2 additional Poison."
- Reworked Bramble card: "Gain 3 (4) Thorns. Reaction (Block): Gain 2 additional Thorns."
- Reworked Trade Up card: "Exhaust 1 card. Procure a random potion. Reaction (Enchanted): Infuse a card in your Hand."
- Reworked Lash Out card: "Deal 6 (8) damage 3 times. Gambit: Hits an additional time. Reaction (Power): Hits an additional time."
- Reworked Drip Feed card: "At the start of your turn, gain 1 (2) Regen. Reaction (Skill): Gain 1 additional Regen."
- Renamed Delayed Reaction to Unstable Compound, reusing the name from the removed Attack
- Changed Unstable Compound card: its power now reads "At the end of this turn, takes N damage." on the turn it detonates, and its damage number and forecast respect Hard to Kill and Intangible
- Changed Sweat It Out card: it now shows "(Apply N Poison.)" during combat
- Changed Fumigate card: it no longer glows gold
- Reworked Citrinitas card: "Deal damage to ALL enemies equal to your Regen (+ 2). Reaction (Exhaust): Deal it again. Add a Rubedo (+) into your Hand. Exhaust."
- Nerfed Siphon card: card draw decreased from 2 -> 1
- Nerfed Nigredo card: Poison decreased from 4(5) -> 3(4)
- Nerfed Rubedo card: Gold decreased from 15(20) -> 5(10)
- Nerfed Froth card: Ferment peak decreased from 4 -> 3
- Buffed Slag card: Block increased from 1(2) -> 2(3)
- Reworked Deep Breath card: "Gain Block equal to Poison on everyone (2 times). Retain." It no longer Infuses a card
- Nerfed Aqua Vitae relic: it no longer grants 1 Regen whenever you use a potion
- Changed the default Brew-only potion chance from 20% -> 15%, and its slider now offers 5%, 10%, 15%, 20%, and 25%
- Changed Soporific potion: it now lands with an impact effect and sound
- Changed how card, relic, and power text writes "Potion": it is now gold and capitalized wherever it names a potion you can obtain, matching the base game

### Removed

- Removed Seep keyword
- Removed Trickle and Unstable Compound cards
- Removed Grind Down and Metabolism cards

## [0.5.2] - 2026-07-25

### Added

- Added Overdose card: "Lose 4 HP. Deal 15 (20) damage. Gambit: Gain 2 Regen." Replaces Last Resort
- Added Sleep On It card: "At the start of your next turn, draw 3 (4) additional cards." Replaces Double Dose
- Added Quicklime card: "Deal 7 (9) damage. Gain 5 (7) Block. Seep: Gain 3 Block." Replaces Tinge
- Added Froth card: "Retain. Ferment. Deal 4 (6) damage. Hits an additional time for each turn fermented." Replaces Full Measure

### Changed

- Reworked Amalgam card into an Attack: "Lose all Poison and Regen. Deal that much damage to ALL enemies X (+1) times. Exhaust."
- Reworked Corrode card: "Apply 6 Poison and 1 (2) Weak to ALL enemies. Gambit: Apply 2 additional Poison."
- Reworked Resolve card: "While Gambit is active, you have 2 (3) additional Strength and Dexterity." Cost decreased from 3(2) -> 1
- Reworked Bloom card into Bramble: "Gain 3 (4) Thorns. Gambit: Gain 2 additional Thorns."
- Reworked Fester card: "Apply 2 (3) Poison. Gain 2 Poison. Poison is triggered against the enemy 1 (2) additional time(s) next turn." Cost decreased from 1 -> 0
- Reworked Deep Cut card: "Deal 7 (10) damage. Draw 2 cards. If this card is Enchanted, apply 1 (2) Weak and 1 (2) Vulnerable."
- Reworked Blood Rush card: "Gain 2 (4) Regen. Lose half your Regen, then draw that many cards."
- Changed Carapace card: Block increased from 6 -> 10, Block per turn fermented changed from 6(9) -> 4(6)
- Buffed Sweat It Out card: it now Ferments, gaining 1 additional Poison for each turn fermented
- Changed Fumigate card: damage is now 1(2), and it keeps Exhaust when Upgraded
- Buffed Cauterize card: damage increased from 5(8) -> 6(9)
- Nerfed Overflow card: damage decreased from 4(5) -> 3(4)
- Nerfed Decant card: damage decreased from 5(7) -> 3(5)
- Nerfed Venom Trance card: Poison threshold increased from 8(6) -> 9(6)
- Renamed Inhale to Deep Breath
- Changed Overflow, Fumigate, and Fighting Spirits cards: they now show "(Hits N times.)" during combat
- Changed Event and Brew-only potions: they now sell to the Merchant for 150 Gold
- Changed the default Brew-only potion chance from 30% -> 20%

### Removed

- Removed Last Resort, Double Dose, Tinge, and Full Measure cards

### Fixed

- Fixed the Ancients repeating an out-of-date conversation after many wins

## [0.5.1] - 2026-07-24

### Changed

- Changed the placeholder card art to show the card type at a glance: Attack
  gradients rise to the right, Skill gradients fall to the right, and Power
  gradients run straight across
- Tuned the placeholder art colors so every card reads apart from its
  neighbors, with less pink and more contrast on the darkest cards

### Fixed

- Fixed banding in the placeholder card and Epoch art. The gradients are now
  smooth instead of stepped, which was most visible on dark cards like Nigredo

## [0.5.0] - 2026-07-24

### Added

- Added the Alchemist to the character stats page in the compendium. The section appears after the first run, like the base characters.
- Added a custom character select sound: an ingredient drops into the brew, flares, and settles into bubbling. The Alchemist no longer borrows the Ironclad's select sound.
- Added hit effects to all 33 attack cards. Each attack now shows an impact that matches the card: stabs for needles, blood splashes for Ichor and Hemorrhage, golden bursts for Azoth and Aureate, shattering for the volatile compounds, and more. Attacks previously showed no hit effect at all
- Added a green poison splash when Double Dose, Tinge, and Flare Up apply Poison on hit, like the Silent's poison cards
- Added fire crackle to Cauterize and Flare Up hits
- A Laced attack now lands with a green splat instead of its normal hit effect, so the added Poison reads at a glance
- Added a damage forecast for Delayed Reaction. The enemy health bar now
  previews the pending hit, the way Poison previews its next tick
- Added hover tooltips that explain the calculated numbers on cards whose damage
  or effect is not obvious from the text.
- Added mod configuration options: an Economy section (potion sell price, Brew
  potion chance, and letting any character sell potions to the Merchant), an
  Accessibility section (show or recolor the damage forecast, toggle card hand
  glows), and a button to reveal the Timeline separately from Unlock All
- Added a placeholder preview image for the mod, which the in-game mod list
  shows until the final art is ready

### Changed

- Updated the Alchemist character select background: the static placeholder image
  is now an animated purple-to-green gradient with slow two-tone swirls, pulsing
  lights, and motes that rise like bubbles above a cauldron, in the style of the
  Random and Regent character splashes
- Updated the 7 Epoch placeholder images: each chapter now has its own gradient
  that matches its story, and all seven ascend toward the top right like the
  climb they tell
- Updated all 97 placeholder card arts so every card has a unique gradient.
  Cards in the same mechanic family (Seep, Ferment, Gambit, potions, and more)
  share a start color, so a family still reads at a glance
- Changed Hemorrhage card upgrade: cost now decreases 1 -> 0 instead of the
  damage increasing from double to triple
- Nerfed Spatter card: it no longer applies 1 Poison on each hit. Its many hits
  make it a strong Laced target instead
- Changed the Seep card glow to a deeper green
- An automatic Infuse, where only one card can be chosen, now previews the
  infused card on screen instead of quietly changing its enchantment icon. This
  also covers Refine, which Infuses the card it chooses
- Reworked Bottled Fury card: the Strength and Dexterity a Potion grants are now
  permanent flat gains, and the amount changed from 2 (3) -> 1 (2). Before, both
  expired at the end of your turn
- Changed the Alchemist's name color to a lighter violet for readability on
  dark backgrounds

### Fixed

- Fixed the Alchemist card icons in the run history showing gray. They now use a
  placeholder purple to match the character
- Fixed Golden Fruit and Unripe Fruit quest cards showing in the Alchemist card
  list. They now appear only under the Quest category, like the tokens. Golden
  Fruit keeps the Alchemist frame and gold energy icon, since only the Alchemist
  can obtain it
- Fixed an Infused Spatter triggering Poison-on-apply effects, such as Sediment,
  twice on each hit
- Fixed the run history and continue-run menus freezing when a saved run named a
  card that a later Alchemist version renamed or removed. The screen now shows
  its out-of-date state and stays usable
- Fixed the macOS black-screen hang on load for modded games. The mod now needs
  BaseLib 3.3.8, which skips the crashing Sentry teardown, so the manual
  override.cfg workaround is no longer needed
- Fixed the Alchemist section on the Character Stats page appearing only every
  other time the page opened

## [0.4.0] - 2026-07-21

### Added

- Added a placeholder Alchemist character-select splash, replacing the borrowed
  Ironclad backdrop until the final art is ready
- Added Neurotoxin card (Rare Attack, cost 2): "Deal 18 (24) damage. Gambit:
  Stun the enemy. Exhaust." Replaces Harvest
- Added Soporific potion (Brew-only, thrown): "Stun an enemy." Replaces Basilisk
  Bile
- Added Delayed Reaction card (Common Skill, cost 1): "At the end of your next
  turn, deal 16 (22) damage to the enemy. Exhaust." Replaces Steep
- Added Unstable Compound card (Uncommon Attack, cost 1): "Deal 16 (22)
  damage. Seep: Add a Toxic into your Hand." Replaces Osmosis

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
- Buffed Carapace card: Block per turn fermented is now a flat 6 (9), up from
  50% (75%) of base Block
- Buffed Rolling Boil card: damage per turn fermented is now a flat 4 (6) per
  hit, up from 50% (75%) of base damage
- Buffed Patient Strike card: damage per turn fermented is now a flat 6 (9), up
  from 75% (100%) of base damage
- Buffed Sinter card: free-cost condition decreased from 7 -> 5 cards in your
  Exhaust Pile
- Buffed Quicksilver Draught potion: it no longer skips the card draw on the
  extra turn; it now simply grants an extra turn
- Reworked Smoke Out card: "Deal 1 (2) damage to ALL enemies. Deals 1 additional
  damage for each card in your Exhaust Pile. Gain 3 (2) Poison." -> "Deal 1
  damage to ALL enemies. Hits an additional time for each card in your Exhaust
  Pile. Exhaust. (Doesn't Exhaust)". It shows the live hit bonus in green
- Buffed Masterwork card: Enchanted threshold decreased from 7 -> 5 cards
- Changed Enrich card: it now Infuses the Draw Pile before it draws, so the
  draw can find the infused cards
- Changed Cauterize card: Regen decreased from 2 -> 1, upgraded damage
  increased from 7 -> 8
- Buffed Volatile Mix card: damage increased from 9 (13) -> 10 (15), damage per
  potion increased from 3 (4) -> 4 (5), and it now reads "If you have no
  potions, gain 1 Poison."
- Changed Golden Touch card: cost increased from 2 -> 3 (2), and its power now
  stacks, so a second copy makes Enchanted cards cost 2 less
- Reworked Refine card: "Draw 1 (2) cards. Infuse a card in your Hand. If this
  card is Enchanted, Infuse 2 cards instead." -> "Upgrade a card in your Hand.
  Infuse it 2 (3) times."
- Reworked Dissect card and renamed it Vivisect: "Deal 8 (12) damage. Draw 2
  cards. If this card is Enchanted, apply 2 Vulnerable." -> "Deal 7 (10)
  damage. Draw 1 card, plus 1 more for each unique debuff on the enemy. If this
  card is Enchanted, apply 1 Weak and 1 Vulnerable."
- Buffed Libation card: it now reads "If this card is Enchanted, gain 2
  Plating."
- Buffed Golden Touch card: it now reads "If this card is Enchanted, this costs
  1 less."
- Reworked Last Resort card into a pure Gambit bonus: "Deal 9 (12) damage.
  Gambit: Deal 5 additional damage. Otherwise, lose 3 HP." -> "Deal 9 (12)
  damage. Gambit: Deal 5 (7) additional damage."
- Changed Puncture card Gambit: it now applies 1 Weak instead of 1 additional
  Vulnerable, so the best case applies both debuffs
- Buffed Siphon card: damage increased from 5 (7) -> 8 (12), in line with other
  draw attacks like Pommel Strike and Photon Cut
- Buffed Azoth card: energy condition decreased from 7 -> 5 cards in your
  Exhaust Pile
- Renamed the Toxic enchantment to Laced, because the base game already has a
  Toxic status card
- Nerfed Distillate token: Regen decreased from 2 (3) -> 1 (2)
- Nerfed Delayed Reaction card: damage decreased from 16 (22) -> 14 (20)

### Removed

- Removed Harvest card, replaced by Neurotoxin
- Removed Osmosis card, replaced by Unstable Compound
- Removed Steep card, replaced by Delayed Reaction
- Removed Basilisk Bile potion, replaced by Soporific

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
