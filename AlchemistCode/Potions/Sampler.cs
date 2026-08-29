using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Potions;

public class Sampler : AlchemistPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        // PotionModel.OnUseWrapper releases the slot before OnUse runs, so this potion is already gone
        // from Owner.Potions. "Including this one" has to add it back, which also makes the floor 1
        var carried = Owner.Potions.Count() + 1;
        var player = target?.Player ?? Owner;
        await PlayerCmd.GainEnergy(carried, player);
        await CardPileCmd.Draw(choiceContext, carried, player);
    }
}
