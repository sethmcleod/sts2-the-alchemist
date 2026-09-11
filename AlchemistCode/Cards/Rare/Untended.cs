using Alchemist.AlchemistCode.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Ferment)]
public class Untended : AlchemistCard
{
    public Untended() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<UntendedPower>(1, 0);
        WithKeyword(CardKeyword.Innate, UpgradeType.Add);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<UntendedPower>(choiceContext, Owner.Creature,
            DynamicVars["UntendedPower"].IntValue, Owner.Creature, this);
    }
}
