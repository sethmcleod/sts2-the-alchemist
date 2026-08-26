using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class Mellow : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Mellow() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<MellowPower>(2, 1);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<MellowPower>(choiceContext, Owner.Creature,
            DynamicVars["MellowPower"].IntValue, Owner.Creature, this);
    }
}
