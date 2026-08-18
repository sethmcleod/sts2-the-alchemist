using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The one-shot spit: a free copy of the dose on one enemy, once. Exhaust is what keeps a big dose
// from becoming a repeatable 0-cost bomb
[CardTheme(CardTheme.Poison)]
public class Retch : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Retch() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Dose", Dose(this) is var p and > 0 ? $" ([green]{p}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var dose = (int)Dose(this);
        if (dose <= 0 || play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, dose, Owner.Creature, this);
    }
}
