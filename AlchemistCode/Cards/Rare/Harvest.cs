using Alchemist.AlchemistCode;
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
        WithTips(_ => new[]
        {
            HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit),
            HoverTipFactory.Static(StaticHoverTip.Fatal),
        });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);

        // Reward on a fatal kill exactly like the base "Fatal" cards (e.g. The Hunt): gate on the real
        // Fatal check (some summoned enemies opt out via ShouldOwnerDeathTriggerFatal) and push the reward
        // straight to the combat room. AddExtraReward accumulates, so each Harvest kill across a multi-enemy
        // fight grants its own potion(s) — no card-level reward-hook bookkeeping needed.
        var killed = attack.Results.SelectMany(r => r).Any(r => r.WasTargetKilled);
        var fatal = play.Target != null && play.Target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());
        if (killed && fatal && Owner.RunState.CurrentRoom is CombatRoom room)
            for (var i = 0; i < DynamicVars["rewards"].IntValue; i++)
                room.AddExtraReward(Owner, new PotionReward(Owner));
    }
}
