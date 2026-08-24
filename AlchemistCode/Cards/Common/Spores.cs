using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Spores : AlchemistCard
{
    public Spores() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(3, 1);
        WithVar("Poison", 1, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        if (play.Target is { IsAlive: true } target)
        {
            PoisonSplash(target);
            await PowerCmd.Apply<PoisonPower>(choiceContext, target,
                DynamicVars["Poison"].IntValue, Owner.Creature, this);
        }
        if (CombatState is not { } combat) return;
        var copy = combat.CloneCard(this);
        var added = await CardPileCmd.Add(copy, PileType.Discard, CardPilePosition.Bottom);
        CardCmd.PreviewCardPileAdd([added]);
    }
}
