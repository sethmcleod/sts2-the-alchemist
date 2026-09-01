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
        WithCards(1, 0);
        WithVar("DecantMax", 2, -1);
        WithVar("DecantCards", 2, 0);
        WithTips(card => Mixing.MixTips(card.IsUpgraded));
    }

    protected override bool Decants => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        // The pick comes before the spend and the bonus draws: a cancelled picker must not
        // spend the level or overdraw
        var matured = DecantFull;
        var mix = await Mixing.Choose(choiceContext, Owner, upgraded: IsUpgraded);
        if (mix == null) return;
        if (matured && TrySpendDecant())
            for (var i = 0; i < DynamicVars["DecantCards"].IntValue; i++)
                await CommonActions.Draw(this, choiceContext);
        await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
    }
}
