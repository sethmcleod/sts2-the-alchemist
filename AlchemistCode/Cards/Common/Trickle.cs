using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Trickle : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public Trickle() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 0);
        WithCards(1, 1);
        WithVar("SeepRegen", 1, 0);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        await CommonActions.Draw(this, choiceContext);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
            DynamicVars["SeepRegen"].BaseValue, Owner.Creature, this);
    }
}
