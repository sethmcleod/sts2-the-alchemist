using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Prime : AlchemistCard, ITranscendenceCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Attack;

    public Prime() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        // The multiplier is 0 or 1, so the face shows the Reaction bonus only while it would land
        WithCalculatedBlock(4, 2, static (card, _) =>
            ((AlchemistCard)card).ReactionActive ? 1 : 0, ValueProp.Move, 3, 0);
        WithTips(_ => Infusion.InfuseTips());
    }

    // BaseLib reads this to build Archaic Tooth's upgrade map. Dusty Tome reads the same map to keep
    // Aureate out of its Ancient pool, so Aureate can come only from Prime
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Aureate>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
    }
}
