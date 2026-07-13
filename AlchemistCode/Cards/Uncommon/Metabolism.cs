using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Metabolism : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Metabolism() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithEnergy(1, 1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<MetabolismPower>(choiceContext, Owner.Creature,
            DynamicVars.Energy.BaseValue, Owner.Creature, this);
        if (IsReduced)
            await CreatureCmd.Heal(Owner.Creature, 3);
    }
}
