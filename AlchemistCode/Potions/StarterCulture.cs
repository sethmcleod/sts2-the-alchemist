using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

public class StarterCulture : AlchemistPotion, IBrewOnly
{
    private const int Times = 3;

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var brewing = PileType.Hand.GetPile(Owner).Cards.OfType<AlchemistCard>()
            .Where(c => c.IsFermentInline).ToList();
        foreach (var card in brewing)
            await card.AdvanceFerment(Times);
        if (brewing.Count > 0)
            CardCmd.Preview(brewing.Cast<CardModel>().ToList());
    }
}
