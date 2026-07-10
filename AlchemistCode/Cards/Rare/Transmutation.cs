using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Transmutation : AlchemistCard
{
    public Transmutation() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // Only the upgrade Infuses, so the tip is only shown there
        WithTips(card => card.IsUpgraded
            ? new IHoverTip[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) }
            : Array.Empty<IHoverTip>());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<TransmutationPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (IsUpgraded)
            await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
    }
}
