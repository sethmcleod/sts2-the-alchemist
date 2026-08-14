using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Gulp : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

public Gulp() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("poison", 3, 0);
        WithCards(1, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
        await CommonActions.Draw(this, choiceContext);
    }
}
