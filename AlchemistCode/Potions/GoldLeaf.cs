using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Potions;

public class GoldLeaf : AlchemistPotion, IBrewOnly
{
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.Static(StaticHoverTip.ReplayStatic) };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = target?.Player ?? Owner;
        if (PileType.Hand.GetPile(player).Cards.Count == 0) return;
        var chosen = await CardSelectCmd.FromHand(choiceContext, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), filter: null, source: this);
        foreach (var card in chosen)
            card.BaseReplayCount++;
    }
}
