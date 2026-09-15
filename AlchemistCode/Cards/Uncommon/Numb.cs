using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.None)]
public class Numb : AlchemistCard
{
    public Numb() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Per", 5, 1);
        WithCalculatedBlock(0, static (card, _) => card.DynamicVars["Per"].IntValue * LooseCount(card), ValueProp.Move);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    private static bool IsLoose(CardModel card, CardModel self) => card != self && !IsBrewing(card);

    private static int LooseCount(CardModel card)
    {
        if (card is not AlchemistCard { IsMutable: true, CombatState: not null, Owner: { } owner }) return 0;
        return PileType.Hand.GetPile(owner).Cards.Count(c => IsLoose(c, card));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var loose = PileType.Hand.GetPile(Owner).Cards.Where(c => IsLoose(c, this)).ToList();
        await CardCmd.Discard(choiceContext, loose);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Per"].IntValue * loose.Count, ValueProp.Move, play);
    }
}
