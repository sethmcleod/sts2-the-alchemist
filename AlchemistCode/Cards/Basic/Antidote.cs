using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Antidote : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Antidote() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithVar("antitoxin", 2, 0);
        WithCards(1, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
        await CommonActions.Draw(this, choiceContext);
    }
}
