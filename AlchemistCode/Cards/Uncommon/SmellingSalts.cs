using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.None)]
public class SmellingSalts : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public SmellingSalts() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithEnergy(1, 1);
        WithVar(new EnergyVar("NextTurn", 1));
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, DynamicVars["NextTurn"].BaseValue,
            Owner.Creature, this);
    }
}
