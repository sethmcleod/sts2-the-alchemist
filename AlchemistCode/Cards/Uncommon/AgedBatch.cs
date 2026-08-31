using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Decant, CardTheme.Mix)]
public class AgedBatch : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public AgedBatch() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("DecantMax", 2, -1);
        WithTips(card => Mixing.MixTips(((AgedBatch)card).ShowsMixPlusTips));
    }

    protected override bool Decants => true;

    private bool ShowsMixPlusTips => DecantFull || (!IsMutable && IsUpgraded);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Peek before the grid so the previews show the + versions, spend only after a real pick:
        // a cancelled picker must not spend the level
        var matured = DecantFull;
        var mix = await Mixing.Choose(choiceContext, Owner, upgraded: matured);
        if (mix == null) return;
        if (matured) TrySpendDecant();
        await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
    }
}
