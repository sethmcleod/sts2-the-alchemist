using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Harvest : AlchemistCard
{
    protected override bool IsMettleCard => true;

    public Harvest() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(10, 5, static (card, _) => ((AlchemistCard)card).IsReduced ? 1 : 0, ValueProp.Move, 3, 0);
        WithVar("rewards", 1, 1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Mettle) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);
        var killed = attack.Results.SelectMany(r => r).Any(r => r.WasTargetKilled);
        if (killed)
            await PowerCmd.Apply<HarvestPower>(choiceContext, Owner.Creature,
                DynamicVars["rewards"].IntValue, Owner.Creature, this);
    }
}
