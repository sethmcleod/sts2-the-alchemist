using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.None)]
public class Croak : AlchemistCard
{
    public Croak() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(20, 6);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(StaticHoverTip.Fatal);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState?.RunState.CurrentRoom is not CombatRoom room) return;
        // Same guard as base The Hunt: a death that a power prevented or converted does not count
        var countsAsFatal = play.Target?.Powers.All(p => p.ShouldOwnerDeathTriggerFatal()) ?? false;
        var attack = await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact"),
            tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay).Execute(choiceContext);
        if (!countsAsFatal || !attack.Results.SelectMany(r => r).Any(r => r.WasTargetKilled)) return;
        // One reward per fatal play, so replays and copies that each kill all stack a reward
        room.AddExtraReward(Owner, new PotionReward(Owner));
    }
}
