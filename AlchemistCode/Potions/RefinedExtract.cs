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

    public override IEnumerable<IHoverTip> ExtraHoverTips => Alchemist.AlchemistCode.Commands.Mixing.MixTips(upgraded: true);

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await Alchemist.AlchemistCode.Commands.Mixing.CreateChosen(choiceContext, Owner, 2, upgraded: true);
    }
}
