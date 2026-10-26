using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Ferment)]
public class TurnSour : AlchemistReshuffledCard
{
    protected override bool Ferments => true;

    public TurnSour() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar(new FermentVar("Weak", 1, "perTurn").WithUpgrade(1));
        WithVar(new FermentVar("Vulnerable", 1, "perTurn").WithUpgrade(1));
        WithVar("perTurn", 1);
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(WeakPower));
        WithTip(typeof(VulnerablePower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } target) return;
        var weak = ((FermentVar)DynamicVars["Weak"]).Total(this, target);
        var vulnerable = ((FermentVar)DynamicVars["Vulnerable"]).Total(this, target);
        await PowerCmd.Apply<WeakPower>(choiceContext, target, weak, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, vulnerable, Owner.Creature, this);
    }
}
