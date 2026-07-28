using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Cauterize : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Attack;

    public Cauterize() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 3);
        WithPower<RegenPower>(1, 0);
        WithVar("ReactionRegen", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var reacted = ReactionActive;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash"), sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
        if (reacted)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
                DynamicVars["ReactionRegen"].BaseValue, Owner.Creature, this);
    }
}
