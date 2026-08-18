using Alchemist.AlchemistCode.Cards.Token;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The Uncommon rung of the Distillate ladder: thinning plus two 0-cost plays this turn, which is why
// it costs 1 where the old Melt Down cost 0
[CardTheme(CardTheme.Transform)]
public class Condense : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Condense() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithUpgradingCardTip<Distillate>();
        WithVar("cards", 1, 1);
        WithTip(StaticHoverTip.Transform);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var selected = await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Draw.GetPile(Owner), Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, DynamicVars["cards"].IntValue));
        foreach (var card in selected)
        {
            var distillate = CombatState.CreateCard<Distillate>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(distillate);
            await CardCmd.Transform(card, distillate);
            await CardPileCmd.Add(distillate, PileType.Hand);
        }
    }
}
