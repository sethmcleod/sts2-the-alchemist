using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Ferment)]
public class Ripen : AlchemistCard
{
    public Ripen() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Turns", 1, 0);
        WithCostUpgradeBy(-1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<RipenPower>(choiceContext, Owner.Creature,
            DynamicVars["Turns"].IntValue, Owner.Creature, this);
    }
}
