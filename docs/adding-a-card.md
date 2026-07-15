# Adding a card, end to end

A complete walkthrough of adding one card to the Alchemist — no prior C# experience
assumed. We'll dissect a real card that already ships ([Tinge](../AlchemistCode/Cards/Common/Tinge.cs)),
then every step generalizes to your own card.

The one rule that governs everything: **a card lives in three places that must stay in
sync** — the code, the localization text, and `cards.csv`. Touch one, touch all
(see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## 0. Names drive everything

Pick the card's display name first — say **Tinge**. The class name (`Tinge`) mechanically
derives everything else:

| Thing | Derived value |
|---|---|
| Model ID (console, tests) | `ALCHEMIST-TINGE` |
| Localization keys | `ALCHEMIST-TINGE.title` / `.description` |
| Portrait file | `Alchemist/images/card_portraits/big/tinge.png` |

Multi-word names snake-case: `DoubleDose` → `ALCHEMIST-DOUBLE_DOSE` → `double_dose.png`.

## 1. The card class

Create `AlchemistCode/Cards/<Rarity>/<Name>.cs`. **Start by copying an existing card
file from the same rarity folder** — the `using` imports and `namespace` line at the top
must match the folder, and copying gets them right for free. Here is Tinge (minus that
header), annotated:

```csharp
public class Tinge : AlchemistCard
{
    protected override bool IsSeepCard => true;          // opts into the Seep keyword

    public Tinge() : base(
        0,                       // energy cost
        CardType.Attack,         // Attack / Skill / Power
        CardRarity.Common,       // pool rarity
        TargetType.AnyEnemy)     // AnyEnemy needs a target; Self doesn't
    {
        WithDamage(3, 1);                 // 3 damage, +1 when upgraded → "3 (4)"
        WithPower<PoisonPower>(2, 0);     // applies 2 Poison, unchanged by upgrade
        WithTip(typeof(RegenPower));      // hover tooltip for the Regen that Seep grants below
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);   // the damage
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);   // the poison
    }

    // The Seep hook — runs at end of turn if the card is still in your hand.
    // Declaring IsSeepCard adds the keyword + tooltip; this is the actual effect.
    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
```

The important idea: **all numbers live in the constructor's `With*` builders** as
`(base, upgradeDelta)` pairs. The engine handles upgrade display, green highlighting,
and enchantment math from those declarations — `OnPlay` just says *what happens*, in
order, using the declared values. Common builders: `WithDamage`, `WithBlock`,
`WithPower<T>`, `WithEnergy`, `WithCards` (draw), `WithVar("name", base, delta)` for
anything custom.

Mod-specific keyword flags (`IsGambitCard`, `IsFermentCard`, `IsSeepCard`) and helpers
live in [AlchemistCard.cs](../AlchemistCode/Cards/AlchemistCard.cs) — read it once, it's
the base class of every card here.

## 2. Localization

Add two keys to [Alchemist/localization/eng/cards.json](../Alchemist/localization/eng/cards.json)
(alphabetical order):

```json
"ALCHEMIST-TINGE.description": "Deal {Damage:diff()} damage.\nApply {PoisonPower:diff()} [gold]Poison[/gold].\n[gold]Seep[/gold]: Gain 2 [gold]Regen[/gold].",
"ALCHEMIST-TINGE.title": "Tinge",
```

- `{Damage:diff()}` / `{PoisonPower:diff()}` render the live number **and** the upgrade
  preview — never hardcode numbers that exist as builders.
- `[gold]…[/gold]` marks keywords; match base-game wording conventions.
- Powers additionally need `.title`, `.description` **and** `.smartDescription` in
  `powers.json` — the build fails without them (by design).

## 3. cards.csv

Add one row to [cards.csv](../cards.csv) — the human-readable design sheet, format
`base (upgraded)`:

```csv
Tinge,Common,Attack,0,Deal 3 (4) damage. Apply 2 Poison. Seep: Gain 2 Regen.
```

## 4. Portrait

Drop a **1000×760 PNG** at `Alchemist/images/card_portraits/big/tinge.png`. While
prototyping, copy any existing portrait as a placeholder. Sizes and conventions for
other asset types (power icons, relics) are in [BUILD.md](../BUILD.md).

## 5. Build and see it

```sh
scripts/dev.sh publish     # build → import new images → publish → verify
```

Launch the game, start an Alchemist run, open the dev console and spawn it:

```
card ALCHEMIST-TINGE
```

(Full model ID, not the display name — bare names silently do nothing.)

## 6. Lock it in with a test

Copy the closest scenario in [scripts/tests/](../scripts/tests/) and adjust: spawn your
card, play it, assert the outcome. For Tinge you'd assert the enemy poison indirectly
and the Seep regen directly — see the README there for what's assertable and the quirks
(settle delays, index offsets).

```sh
scripts/dev.sh test tinge
```

A card that changes numbers later without its test changing is how regressions ship —
this is the cheapest insurance in the repo.

## Going deeper

- Hooks, powers, relics, dynamic vars, Harmony patching: the
  [sts2-modding-mcp guides](https://github.com/sethmcleod/sts2-modding-mcp) cover the
  general machinery.
- Design conventions (pricing risk, when numbers must show live totals):
  [CONTRIBUTING.md](../CONTRIBUTING.md).
- Something behaving weirdly: [troubleshooting.md](troubleshooting.md).
