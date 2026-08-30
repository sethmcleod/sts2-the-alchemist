using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class Vent : AlchemistCard
{
    public Vent() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithVar("Mult", 2, 1);
        WithCalculatedVar("Poison", 0, 0, static (card, _) =>
            Dose(card) * ((card as Vent)?.DynamicVars["Mult"].IntValue ?? 0));
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var dose = (int)Dose(this);
        if (dose <= 0 || play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -dose, Owner.Creature, this);
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, dose * DynamicVars["Mult"].IntValue, Owner.Creature, this);
    }
}
