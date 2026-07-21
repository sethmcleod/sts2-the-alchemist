using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Siphon : AlchemistCard
{
    public Siphon() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, 4);
        WithCards(2, 0);
    }

    internal override bool GainsEffectWhenEnchanted => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var draw = DynamicVars["Cards"].IntValue + (IsEnchanted ? 1 : 0);
        await CardPileCmd.Draw(choiceContext, draw, Owner);
    }
}
