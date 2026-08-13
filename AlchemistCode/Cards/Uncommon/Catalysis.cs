using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Catalysis : AlchemistCard
{
    protected override bool ShowsReactionTip => true;

    public Catalysis() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<CatalysisPower>(choiceContext, Owner.Creature,
            DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }
}
