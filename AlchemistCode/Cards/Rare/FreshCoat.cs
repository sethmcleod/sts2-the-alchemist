using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class FreshCoat : AlchemistCard
{
    public FreshCoat() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<FreshCoatPower>(choiceContext, Owner.Creature,
            DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }
}
