using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Vent : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Vent() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedVar("Poison", 0, 2, static (card, _) => Dose(card));
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var dose = (int)Dose(this);
        if (dose <= 0 || play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -dose, Owner.Creature, this);
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, dose * 2, Owner.Creature, this);
    }
}
