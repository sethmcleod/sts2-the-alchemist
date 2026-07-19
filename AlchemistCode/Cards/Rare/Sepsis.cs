using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Sepsis : AlchemistCard
{
    public Sepsis() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("percent", 25, 25);
        WithPower<PoisonPower>(3, -1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
        await PowerCmd.Apply<SepsisPower>(choiceContext, Owner.Creature,
            DynamicVars["percent"].IntValue, Owner.Creature, this);
    }
}
