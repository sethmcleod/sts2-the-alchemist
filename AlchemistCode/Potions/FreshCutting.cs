using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Potions;

// A rooted cutting keeps growing after it is drunk: the only potion in either pool whose effect
// advances over the turns that follow
public class FreshCutting : AlchemistPotion, IBrewOnly
{
    private const int Turns = 3;

    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromCard<FumingMix>(),
        HoverTipFactory.FromCard<AcridMix>(),
        HoverTipFactory.FromCard<SparklingMix>(),
    };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var drinker = target ?? Owner.Creature;
        await PowerCmd.Apply<FreshCuttingPower>(choiceContext, drinker, Turns, Owner.Creature, null);
    }
}
