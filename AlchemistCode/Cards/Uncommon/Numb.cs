using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.None)]
public class Numb : AlchemistCard
{
    public Numb() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Per", 4, 1);
        WithCalculatedBlock(0, static (card, _) => card.DynamicVars["Per"].IntValue * OtherHandCount(card), ValueProp.Move);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards.Where(c => c != this).ToList();
        await CardCmd.Discard(choiceContext, hand);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Per"].IntValue * hand.Count, ValueProp.Move, play);
    }
}
