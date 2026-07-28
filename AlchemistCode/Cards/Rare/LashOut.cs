using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class LashOut : AlchemistCard
{
    private const int Hits = 3;

    protected override bool IsGambitCard => true;
    protected override ReactionCondition Reaction => ReactionCondition.Power;

    public LashOut() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
    }

    // Both riders stack, so the count runs 3 to 5 and the card face has to say which
    private int HitCount => Hits + (IsReduced ? 1 : 0) + (ReactionActive ? 1 : 0);

    // CombatState, not IsMutable: the compendium's upgraded preview is a mutable copy with no Owner, so
    // an IsMutable check alone still renders the line outside combat. Only show a count that differs from
    // the 3 already printed above it, or the line is noise on every draw
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HitsLine",
            HitsLine(CombatState != null && HitCount > Hits ? HitCount : 0));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, HitCount, vfx: HitVfx("vfx/vfx_attack_slash"))
            .Execute(choiceContext);
    }
}
