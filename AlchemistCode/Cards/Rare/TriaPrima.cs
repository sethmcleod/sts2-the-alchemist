using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class TriaPrima : AlchemistCard
{
    protected override bool HasEnergyCostX => true;
    protected override bool IsFermentCard => true;

    protected override string FermentTotalText
    {
        get
        {
            if (FermentTurns <= 0) return "";
            var total = (int)DynamicVars["Strength"].BaseValue * FermentTurns;
            return $" (Gains [green]{total}[/green] Strength.)";
        }
    }

    public TriaPrima() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Strength", 1, 1); // Ferment: Strength per fermented turn
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
        WithTip(typeof(StrengthPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var x = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (x > 0)
        {
            foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, x, Owner.Creature, this);
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);
        }

        var strength = DynamicVars["Strength"].BaseValue * ConsumeFermentTurns();
        if (strength > 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, strength, Owner.Creature, this);
    }
}
