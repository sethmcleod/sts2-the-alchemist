using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The frog spits a copy of its dose at one enemy. Transfer without spending, so the readers you play
// afterwards still have the number
[CardTheme(CardTheme.Poison)]
public class Spew : AlchemistCard
{
    public Spew() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
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
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        var dose = (int)Dose(this);
        if (dose <= 0 || play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, dose, Owner.Creature, this);
    }
}
