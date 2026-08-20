using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Ferment, CardTheme.Poison)]
public class SweatItOut : AlchemistCard
{
    protected override bool Ferments => true;

    public SweatItOut() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("SelfPoison", 2, 0);
        WithVar("FermentPoison", 2, 1);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    private int FermentPoison =>
        (int)DynamicVars["SelfPoison"].BaseValue
        + DynamicVars["FermentPoison"].IntValue * FermentTurns;

    private int PoisonToApply =>
        IsMutable && CombatState != null
            ? Owner.Creature.GetPowerAmount<PoisonPower>() + FermentPoison
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
            FermentPoison, Owner.Creature, this);
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poison <= 0) return;
        foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, poison, Owner.Creature, this);
    }
}
