using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Quench : AlchemistCard
{
    public Quench() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    internal override bool GainsEffectWhenEnchanted => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        // One call with the full count. A second call double-subscribes the selection events
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, IsEnchanted ? 2 : 1);
    }
}
