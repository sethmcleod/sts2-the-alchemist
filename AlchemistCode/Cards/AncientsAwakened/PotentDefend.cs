using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

[CardTheme(CardTheme.Mix)]
public class PotentDefend : AlchemistAncientsCard
{
    public override CardPoolModel VisualCardPool =>
        AncientsAwakenedMod.PerfectedPoolOr(ModelDb.CardPool<Character.AlchemistCardPool>());

    protected internal override bool PlaysCastAnimation => false;

    public PotentDefend() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithBlock(5, 3);
        WithTags(CardTag.Defend);
        WithTips(_ => Mixing.MixTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await Mixing.CreateRandom(choiceContext, Owner, source: this);
    }
}
