using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public partial class AntitoxinPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;

    // An invisible power loses its hover tips, so AntitoxinBarPatches adds them back
    protected override bool IsVisibleInternal => false;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    internal static AbstractModel? GrantingSource { get; private set; }

    public static async Task GrantFrom(AbstractModel source, PlayerChoiceContext choiceContext, Creature target,
        decimal amount, Creature? applier)
    {
        var outer = GrantingSource;
        GrantingSource = source;
        try
        {
            await PowerCmd.Apply<AntitoxinPower>(choiceContext, target, amount, applier, null);
        }
        finally
        {
            GrantingSource = outer;
        }
    }

    // The canonical model's Id lets MegaTryAddingTip de-duplicate the tip
    public static IHoverTip TipFor(Creature creature)
    {
        var model = ModelDb.Power<AntitoxinPower>();
        var text = model.Description.GetFormattedText();

        if ((CombatManager.Instance?.IsInProgress ?? false)
            && creature.GetPower<PoisonPower>() is { Amount: > 0 } poison)
        {
            var line = new LocString("powers", "ALCHEMIST-ANTITOXIN_POWER.forecast");
            line.Add("Poison", poison.Amount);
            line.Add("Damage", poison.CalculateTotalDamageNextTurn());
            text += "\n" + line.GetFormattedText();
        }

        return new HoverTip(model, text, isSmart: false);
    }

    private bool IsPoisonTick(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        target == Owner
        && AntitoxinRules.IsPoisonTick(Owner, amount, props, dealer, cardSource);

    // Keep pure: PoisonPower.CalculateTotalDamageNextTurn previews through this hook
    internal decimal Absorb(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        IsPoisonTick(target, amount, props, dealer, cardSource)
            ? -Math.Min(Amount, (int)amount)
            : 0m;

    // Amount is post-Absorb and Poison decrements after the hit, so stack - amount is what was held
    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;

        AntitoxinRules.ClearTickAbsorb(Owner);
        if (!AntitoxinRules.HasPoisonTickShape(props, dealer, cardSource)) return;

        var stack = Owner.GetPowerAmount<PoisonPower>();
        var absorbed = stack - (int)amount;
        if (absorbed <= 0 || absorbed > Amount) return;

        AntitoxinRules.MarkAbsorbed(Owner, absorbed);
        Analytics.RunCounters.Tally(Owner.Player,
            (int)amount == 0 ? Analytics.RunCounters.TickCovered : Analytics.RunCounters.TickBled);
    }

    // Late, so the PoisonPower tick in AfterSideTurnStart lands before the cover thins
    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Amount <= 0) return;
        if (Owner.Player is { PlayerCombatState.TurnNumber: 1 }) return;
        Analytics.RunCounters.Tally(Owner.Player, Analytics.RunCounters.AntitoxinDecayed);
        await PowerCmd.Decrement(this);
    }
}
