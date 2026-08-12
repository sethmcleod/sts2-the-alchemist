using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Overflow : AlchemistCard
{
    public Overflow() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    private int PoisonNow =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<PoisonPower>() : 0;

    protected override bool ConditionalGlow => PoisonNow > 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Dose", PoisonNow is var p and > 0 ? $" ([green]{p}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var dose = PoisonNow;
        if (dose <= 0) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, dose, Owner.Creature, this);
    }
}
