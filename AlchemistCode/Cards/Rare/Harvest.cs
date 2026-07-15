using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Harvest : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Harvest() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(10, 5, static (card, _) => ((AlchemistCard)card).IsReduced ? 1 : 0, ValueProp.Move, 3, 0);
        WithVar("rewards", 1, 1);
        WithTips(_ => new[] { HoverTipFactory.Static(StaticHoverTip.Fatal) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Decide Fatal eligibility from the target's powers BEFORE the attack — some
        // summoned enemies opt out via ShouldOwnerDeathTriggerFatal. Reading Powers
        // *after* the kill races with death-cleanup tearing the collection down
        // (InvalidOperationException: collection modified); snapshot it here while stable.
        var fatalEligible = play.Target != null
            && play.Target.Powers.ToList().All(p => p.ShouldOwnerDeathTriggerFatal());

        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);

        var killed = attack.Results.SelectMany(r => r).Any(r => r.WasTargetKilled);
        if (killed && fatalEligible && Owner.RunState.CurrentRoom is CombatRoom room)
            for (var i = 0; i < DynamicVars["rewards"].IntValue; i++)
                room.AddExtraReward(Owner, new PotionReward(Owner));
    }
}
