using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Toughen : AlchemistCard
{
    private const int PerPoison = 2;

    protected internal override bool PlaysCastAnimation => false;

    public Toughen() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(4, static (card, _) =>
            card.Owner.Creature.GetPowerAmount<PoisonPower>() * PerPoison, ValueProp.Move, 2, 0);
        WithTip(typeof(PoisonPower));
    }

    private int Dose =>
        IsMutable && CombatState != null ? Owner.Creature.GetPowerAmount<PoisonPower>() : 0;

    protected override bool ConditionalGlow => Dose > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // CalculatedBlock reads the dose live, so clearing the Poison first pays out the floor alone
        await CommonActions.CardBlock(this, play);

        var spent = Dose;
        if (spent > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -spent, Owner.Creature, this);
    }
}
