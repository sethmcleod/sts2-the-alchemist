using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Adapt : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Adapt() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(7, 3);
        WithEnergy(1, 0);
        WithTip(typeof(AntitoxinPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && AntitoxinRules.AbsorbedThisTurn(Owner.Creature);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (AntitoxinRules.AbsorbedThisTurn(Owner.Creature))
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}
