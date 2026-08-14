using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Rot : AlchemistCard
{
    public Rot() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Amount", 5, 2);
        WithVar("OnPlay", 3, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState is { } combat)
            foreach (var enemy in combat.Enemies.Where(e => e.IsAlive).ToList())
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy,
                    DynamicVars["OnPlay"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<RotPower>(choiceContext, Owner.Creature, DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
