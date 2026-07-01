using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Accretion : AlchemistCard
{
    public Accretion() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Regen", 1, 1);
        WithTip(typeof(RegenPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Mettle) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AccretionPower>(choiceContext, Owner.Creature,
            DynamicVars["Regen"].IntValue, Owner.Creature, this);
    }
}
