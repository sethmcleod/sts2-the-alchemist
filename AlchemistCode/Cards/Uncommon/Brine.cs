using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Poison)]
public class Brine : AlchemistCard
{
    protected override bool Ferments => true;

    public Brine() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(0, static (_, target) => target?.GetPowerAmount<PoisonPower>() ?? 0m, ValueProp.Move);
        WithVar(new FermentVar("Hits", 1, 1).WithUpgrade(1));
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    private int Hits => ((FermentVar)DynamicVars["Hits"]).Total(this, null);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_slime_impact"),
            tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
    }
}
