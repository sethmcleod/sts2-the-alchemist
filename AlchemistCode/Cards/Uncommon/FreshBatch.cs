using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class FreshBatch : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public FreshBatch() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<FreshBatchPower>(2, 1);
        WithTips(_ => Mixing.MixTips(upgraded: true));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<FreshBatchPower>(choiceContext, this);
    }
}
