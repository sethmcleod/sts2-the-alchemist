using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Cauterize : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Cauterize() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 3);
        WithPower<RegenPower>(1, 0);
        WithVar("GambitRegen", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash"), sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
        if (IsReduced)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
                DynamicVars["GambitRegen"].BaseValue, Owner.Creature, this);
    }
}
