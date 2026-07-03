using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Refine : AlchemistCard
{
    public Refine() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        // Live damage = base 8 (11) + your current Regen.
        WithCalculatedDamage(8, static (card, _) => card.Owner.Creature.GetPowerAmount<RegenPower>(),
            ValueProp.Move, 3, 0);
        WithTip(typeof(RegenPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
    }
}
