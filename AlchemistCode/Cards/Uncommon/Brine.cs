using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Poison)]
public class Brine : AlchemistCard
{
    protected override bool Ferments => true;

    private const int Hits = 2;

    public Brine() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(0,
            static (card, target) => (target?.GetPowerAmount<PoisonPower>() ?? 0m) + Ripened(card), ValueProp.Move);
        WithVar("PerTurn", 2, 1);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    private static decimal Ripened(CardModel card) =>
        card is AlchemistCard { IsMutable: true, CombatState: not null } ferment
            ? ferment.FermentTurns * card.DynamicVars["PerTurn"].IntValue
            : 0m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_slime_impact"),
            tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
    }
}
