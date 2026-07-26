using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class SweatItOut : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public SweatItOut() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("SelfPoison", 2, 1);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].BaseValue + FermentTurns, Owner.Creature, this);
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (Owner.Creature.HasPower<PoisonPower>())
            await PowerCmd.Remove<PoisonPower>(Owner.Creature);
        if (poison <= 0) return;
        foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, poison, Owner.Creature, this);
    }
}
