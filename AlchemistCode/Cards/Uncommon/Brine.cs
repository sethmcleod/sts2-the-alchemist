using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Poison)]
public class Brine : AlchemistCard
{
    protected override bool Ferments => true;

    public Brine() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(6, static (card, _) => 3m * ((AlchemistCard)card).FermentTurns, ValueProp.Move, 2, 0);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (FermentTurns > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, FermentTurns, Owner.Creature, this);
    }
}
