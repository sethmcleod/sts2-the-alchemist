using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

// Cheap Block bought with self-Poison. The upgrade lowers the Poison rather than raising the Block,
// so the card reads as the same wall for a smaller price
public class CheapCover : AlchemistCard
{
    public CheapCover() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 0);
        WithVar("poison", 2, -1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
    }
}
