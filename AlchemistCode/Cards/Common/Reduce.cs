using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using Alchemist.AlchemistCode.Cards.Token;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

// The Common rung of the Distillate ladder. Block on rate alone; the thinning reaches into the
// Discard, so it lands next cycle rather than now, which is the right size of upside for Common
[CardTheme(CardTheme.Transform)]
public class Reduce : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Reduce() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(5, 3);
        WithUpgradingCardTip<Distillate>();
        WithTip(StaticHoverTip.Transform);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (CombatState == null) return;
        var discard = PileType.Discard.GetPile(Owner);
        if (discard.Cards.Count == 0) return;
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, discard, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1))).FirstOrDefault();
        if (chosen == null) return;
        var distillate = CombatState.CreateCard<Distillate>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(distillate);
        await CardCmd.Transform(chosen, distillate);
    }
}
