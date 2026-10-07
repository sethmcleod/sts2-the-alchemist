using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class Chrysopoeia : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Chrysopoeia() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(1, 1);
        WithVar(new EnergyVar("Bonus", 1));
        WithVar("Per", 4, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        var steps = poison / DynamicVars["Per"].IntValue;
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue + DynamicVars["Bonus"].IntValue * steps, Owner);
        if (poison > 0) await PowerCmd.Remove<PoisonPower>(Owner.Creature);
    }
}
