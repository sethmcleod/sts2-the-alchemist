using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Overdose : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Overdose() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(15, 5);
        WithVar("hpLoss", 4, 0);
        WithVar("GambitRegen", 2, 0);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Read Gambit before the HP loss. Otherwise this card's own cost could push you under the
        // line and pay you for it, which would make the Regen unconditional in practice
        var gambit = IsReduced;
        await LoseHp(choiceContext, DynamicVars["hpLoss"].IntValue);
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_blunt")).Execute(choiceContext);
        if (gambit)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
                DynamicVars["GambitRegen"].IntValue, Owner.Creature, this);
    }
}
