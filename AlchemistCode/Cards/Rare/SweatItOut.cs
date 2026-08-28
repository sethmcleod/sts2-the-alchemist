using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class SweatItOut : AlchemistCard
{
    public SweatItOut() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("SelfPoison", 4, 2);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    private int PoisonToApply =>
        IsMutable && CombatState != null
            ? Owner.Creature.GetPowerAmount<PoisonPower>() + DynamicVars["SelfPoison"].IntValue
            : 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("ApplyLine",
            PoisonToApply is var n and > 0 ? PreviewLine("ALCHEMIST-APPLY_LINE", "Amount", n) : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poison <= 0) return;
        foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, poison, Owner.Creature, this);
    }
}
