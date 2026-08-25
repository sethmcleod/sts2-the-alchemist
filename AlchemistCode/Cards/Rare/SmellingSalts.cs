using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Antitoxin)]
public class SmellingSalts : AlchemistCard
{
    public SmellingSalts() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(1, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    // Forward-looking on purpose: it lights when the dose already outruns the capacity, so the next
    // tick is going to get through and this card is going to pay
    protected override bool ConditionalGlow =>
        this is { IsMutable: true, Owner.Creature: { } c }
        && c.GetPowerAmount<PoisonPower>() > c.GetPowerAmount<AntitoxinPower>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SmellingSaltsPower>(choiceContext, Owner.Creature,
            DynamicVars["Energy"].IntValue, Owner.Creature, this);
    }
}
