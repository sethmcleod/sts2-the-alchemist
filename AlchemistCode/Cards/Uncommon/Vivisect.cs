using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Vivisect : AlchemistCard
{
    public Vivisect() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithVar("Cards", 2, 0);
        WithVar("Debuff", 1, 1);
        WithTip(typeof(WeakPower));
        WithTip(typeof(VulnerablePower));
    }

    internal override bool GainsEffectWhenEnchanted => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, Owner);
        if (IsEnchanted && play.Target is { IsAlive: true })
        {
            var debuff = DynamicVars["Debuff"].IntValue;
            await PowerCmd.Apply<WeakPower>(choiceContext, play.Target, debuff, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, play.Target, debuff, Owner.Creature, this);
        }
    }
}
