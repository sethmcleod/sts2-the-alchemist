using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Prime : AlchemistCard, ITranscendenceCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Attack;

    public Prime() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(4, 3);
        WithTips(_ => Infusion.InfuseTips());
    }

    // BaseLib reads this to build Archaic Tooth's upgrade map. Dusty Tome reads the same map to keep
    // Aureate out of its Ancient pool, so Aureate can come only from Prime
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Aureate>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var reacted = ReactionActive;
        await CommonActions.CardBlock(this, play);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, reacted ? 2 : 1);
    }
}
