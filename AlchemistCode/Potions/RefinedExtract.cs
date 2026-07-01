using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Alchemist.AlchemistCode.Cards.Token;

namespace Alchemist.AlchemistCode.Potions;

public class RefinedExtract : AlchemistPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromCard<Distillate>() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var combatState = Owner.Creature.CombatState!;
        var d1 = combatState.CreateCard<Distillate>(Owner);
        var d2 = combatState.CreateCard<Distillate>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(d1, PileType.Hand, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(d2, PileType.Hand, Owner);
    }
}
