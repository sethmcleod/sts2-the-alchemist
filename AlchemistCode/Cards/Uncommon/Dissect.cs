using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Dissect : AlchemistCard
{
    public Dissect() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 4);
        WithCards(2, 0);
        WithTip(typeof(VulnerablePower));
    }

    protected override bool ConditionalGlow => IsEnchanted;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, Owner);
        if (IsEnchanted)
            await PowerCmd.Apply<VulnerablePower>(choiceContext, play.Target!, 2, Owner.Creature, this);
    }
}
