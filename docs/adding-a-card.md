# How to add a card, from start to end

This document shows how to add one card to the Alchemist. It does not assume experience
with C#. It examines [Tinge](../AlchemistCode/Cards/Common/Tinge.cs), a card that the mod
already contains. Each step also applies to a new card.

> [!IMPORTANT]
> **A card is in three places, and the three places must agree**: the code, the
> localization text, and `cards.csv`. If you change one place, change all three places.
> See [CONTRIBUTING.md](../CONTRIBUTING.md).

## 0. Names drive everything

Select the display name of the card first, for example **Tinge**. The class name
(`Tinge`) gives all the other values:

| Thing | Derived value |
|---|---|
| Model id (console, tests) | `ALCHEMIST-TINGE` |
| Localization keys | `ALCHEMIST-TINGE.title` / `.description` |
| Portrait file | `Alchemist/images/card_portraits/big/tinge.png` |

Names with more than one word use snake case: `DoubleDose` → `ALCHEMIST-DOUBLE_DOSE` →
`double_dose.png`.

## 1. The card class

Create `AlchemistCode/Cards/<Rarity>/<Name>.cs`. **First, copy a card file that exists in
the same rarity folder.** The `using` imports and the `namespace` line at the top must
agree with the folder. A copy gives you the correct values. The example below shows Tinge
with comments. It does not show the header.

```csharp
public class Tinge : AlchemistCard
{
    protected override bool IsSeepCard => true;          // adds the Seep keyword

    public Tinge() : base(
        0,                       // energy cost
        CardType.Attack,         // Attack / Skill / Power
        CardRarity.Common,       // pool rarity
        TargetType.AnyEnemy)     // AnyEnemy needs a target, Self does not
    {
        WithDamage(3, 1);                 // 3 damage, +1 after an upgrade, shows "3 (4)"
        WithPower<PoisonPower>(2, 0);     // applies 2 Poison, an upgrade does not change it
        WithTip(typeof(RegenPower));      // tooltip for the Regen that Seep gives below
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);   // the damage
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);   // the Poison
    }

    // The Seep hook runs at the end of the turn if the card is still in your hand.
    // IsSeepCard adds the keyword and the tooltip. This method is the effect.
    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
```

The important rule is this: **all the numbers are in the `With*` builders in the
constructor**. Each number is a `(base, upgradeDelta)` pair. The engine uses these
declarations for the upgrade display, the green highlight, and the enchantment
calculation. Thus `OnPlay` gives only the sequence of effects, and it uses the declared
values. The common builders are `WithDamage`, `WithBlock`, `WithPower<T>`, `WithEnergy`,
`WithCards` (draw), and `WithVar("name", base, delta)` for other values.

The keyword flags of this mod (`IsGambitCard`, `IsFermentCard`, `IsSeepCard`) and the
helper methods are in [AlchemistCard.cs](../AlchemistCode/Cards/AlchemistCard.cs). Read
this file one time. It is the base class of every card in the mod.

## 2. Localization

Add two keys to
[Alchemist/localization/eng/cards.json](../Alchemist/localization/eng/cards.json). Keep
the keys in alphabetical order.

```json
"ALCHEMIST-TINGE.description": "Deal {Damage:diff()} damage.\nApply {PoisonPower:diff()} [gold]Poison[/gold].\n[gold]Seep[/gold]: Gain 2 [gold]Regen[/gold].",
"ALCHEMIST-TINGE.title": "Tinge",
```

- `{Damage:diff()}` and `{PoisonPower:diff()}` show the live number **and** the upgrade
  preview. Never write a number directly if a builder supplies it.
- `[gold]…[/gold]` marks a keyword. Use the same words as the base game.
- Powers also need `.title`, `.description` **and** `.smartDescription` in `powers.json`.
  The build fails without these keys. This behavior is intentional.

## 3. cards.csv

Add one row to [cards.csv](../cards.csv). This file is the design sheet for humans. The
format for numbers is `base (upgraded)`.

```csv
Tinge,Common,Attack,0,Deal 3 (4) damage. Apply 2 Poison. Seep: Gain 2 Regen.
```

## 4. Portrait

Put a **1000×760 PNG** file at `Alchemist/images/card_portraits/big/tinge.png`. For a
first version, use any other portrait as a placeholder. [BUILD.md](../BUILD.md) gives
the sizes and the rules for the other asset types, for example power icons and relics.

## 5. Build and see it

```sh
scripts/dev.sh publish     # build → import new images → publish → verify
```

Start the game. Start an Alchemist run. Open the dev console. Then use this command to
spawn the card:

```
card ALCHEMIST-TINGE
```

> [!NOTE]
> Use the full model id, not the display name. A name alone, for example `card Tinge`,
> does nothing. The console reports success for both commands.

## 6. Add a test

Copy the scenario in [scripts/tests/](../scripts/tests/) that is the most similar. Change
the copy for the new card. The scenario must spawn the card, play the card, then assert
the result. For Tinge, assert the Poison on the enemy indirectly. Assert the Regen from
Seep directly.

The README in that folder lists the values that you can assert. It also lists the known
problems, for example settle delays and index offsets.

```sh
scripts/dev.sh test tinge
```

> [!TIP]
> If the numbers of a card change, change its test also. If you do not, a regression can
> go into a release.

## More information

- The [sts2-modding-mcp guides](https://github.com/sethmcleod/sts2-modding-mcp) explain
  the general systems: hooks, powers, relics, DynamicVars, and the Harmony patch.
- [CONTRIBUTING.md](../CONTRIBUTING.md) gives the design rules. These rules include the
  price of risk, and when a number must show a live total.
- [troubleshooting.md](troubleshooting.md) gives the fixes for known problems.
