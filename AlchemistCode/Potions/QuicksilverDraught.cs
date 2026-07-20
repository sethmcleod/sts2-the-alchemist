using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Potions;

public class QuicksilverDraught : AlchemistPotion, IBrewOnly
{
    // Event rarity keeps this out of every rarity-filtered roll and files it under "Special" in the
    // potion lab, which is where a potion that is "created by other means" belongs
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        // The base game's invisible extra-turn counter, the same one Ambergris applies
        await PowerCmd.Apply<AmbergrisPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
        // Amount 2: one stack burns at the end of this turn, the last stack blocks the
        // start-of-turn hand draw on the extra turn and clears at its end
        await PowerCmd.Apply<QuicksilverFatiguePower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null);
    }
}
