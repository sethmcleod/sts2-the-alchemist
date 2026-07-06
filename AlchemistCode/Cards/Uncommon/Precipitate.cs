using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Precipitate : AlchemistCard
{
    public Precipitate() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(5, 1); // 5 (6) Block, gained twice — an infused Nimble bonus lands on each gain
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.CardBlock(this, play); // twice: two separate Block gains
        await PowerCmd.Apply<PrecipitatePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
