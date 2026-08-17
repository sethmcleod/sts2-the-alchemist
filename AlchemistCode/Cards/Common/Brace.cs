using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Brace : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Brace() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(5, 3);
        WithEnergy(1, 0);
        WithTip(typeof(PoisonPower));
    }

    private bool Dosed =>
        IsMutable && Owner != null && Owner.Creature.GetPowerAmount<PoisonPower>() > 0;

    protected override bool ConditionalGlow => Dosed;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (Dosed) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}
