using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Amalgam : AlchemistCard
{
    protected override bool HasEnergyCostX => true;
    protected override bool IsFermentCard => true;

    // The loc feeds these two strings into its IfUpgraded branches, so the upgraded branch always carries
    // at least +1 and the base branch hides at +0. The upgrade preview renders the upgraded branch with
    // FermentTurns still 0, which is what shows the player "+1"
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("XBonusUpgraded", $"+{FermentTurns + 1}");
        description.Add("XBonusBase", FermentTurns > 0 ? $"+{FermentTurns}" : "");
    }

    public Amalgam() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var x = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0) + FermentTurns;
        if (x > 0)
        {
            foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, x, Owner.Creature, this);
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);
        }
    }
}
