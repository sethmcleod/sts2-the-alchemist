using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Decant : AlchemistCard
{
    public Decant() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) => card.Owner.Creature.GetPowerAmount<RegenPower>(),
            ValueProp.Move, 2, 0);
        WithTip(typeof(RegenPower));
        WithTips(_ => new[] { HoverTipFactory.FromCard<Distillate>() });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await AlchemistCardCmd.GiveCard<Distillate>(this, 1, forceUpgrade: false);
    }
}
