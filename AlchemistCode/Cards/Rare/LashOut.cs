using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class LashOut : AlchemistCard
{
    private const int Hits = 3;

    protected override bool IsGambitCard => true;
    protected override ReactionCondition Reaction => ReactionCondition.Power;

    public LashOut() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithTip(typeof(RegenPower));
    }

    // Only the Reaction moves the count now. Gambit pays in Regen instead, so fighting on at a third HP
    // also buys the way back out of it
    private int HitCount => Hits + (ReactionActive ? 1 : 0);

    // CombatState, not IsMutable: the compendium's upgraded preview is a mutable copy with no Owner. Only
    // show a count that differs from the 3 already printed above it, or the line is noise on every draw
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HitsLine",
            HitsLine(CombatState != null && HitCount > Hits ? HitCount : 0));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = HitCount;
        var gambit = IsReduced;

        await CommonActions.CardAttack(this, play, hits, vfx: HitVfx("vfx/vfx_attack_slash"))
            .Execute(choiceContext);

        if (gambit)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, hits, Owner.Creature, this);
    }
}
