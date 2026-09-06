using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Potions;

// The base Ashwater pattern: one hand-selection round with the potion as its source
public class Reduction : AlchemistPotion, IBrewOnly
{
    private const int MaxCards = 3;

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = target?.Player ?? Owner;
        if (PileType.Hand.GetPile(player).Cards.Count == 0) return;
        var chosen = (await CardSelectCmd.FromHand(choiceContext, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, MaxCards), filter: null, source: this)).ToList();
        foreach (var card in chosen)
            await CardCmd.Exhaust(choiceContext, card);
        if (chosen.Count == 0) return;
        await PlayerCmd.GainEnergy(chosen.Count, player);
        await CardPileCmd.Draw(choiceContext, chosen.Count, player);
    }
}
