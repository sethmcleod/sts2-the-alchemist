using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Amalgam : AlchemistCard
{
    protected override bool HasEnergyCostX => true;
    protected override bool IsFermentCard => true;

    protected override string FermentTotalText
    {
        get
        {
            if (FermentTurns <= 0) return "";
            return $" (X is [green]{FermentTurns}[/green] higher.)";
        }
    }

    public Amalgam() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Ferment increases X (energy spent) by 1 per fermented turn, boosting both Poison and Regen.
        var x = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0) + ConsumeFermentTurns();
        if (x > 0)
        {
            foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, x, Owner.Creature, this);
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);
        }
    }
}
