using Alchemist.AlchemistCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class SmellingSalts : AlchemistCard
{
    public SmellingSalts() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithEnergy(1, 0);
        WithVar("Threshold", 2, 0);
        WithKeyword(CardKeyword.Innate, UpgradeType.Add);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    protected override bool ConditionalGlow =>
        this is { IsMutable: true, Owner.Creature: { } c }
        && c.GetPowerAmount<PoisonPower>() >= DynamicVars["Threshold"].IntValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SmellingSaltsPower>(choiceContext, Owner.Creature,
            DynamicVars["Energy"].IntValue, Owner.Creature, this);
        if (Owner.Creature.GetPower<SmellingSaltsPower>() is { } salts)
            salts.LowerThreshold(DynamicVars["Threshold"].IntValue);
    }
}
