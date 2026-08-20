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

    protected override bool ConditionalGlow =>
        Owner?.Creature is { } c && c.HasPower<PoisonPower>() && c.HasPower<AntitoxinPower>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SmellingSaltsPower>(choiceContext, Owner.Creature,
            DynamicVars["Energy"].IntValue, Owner.Creature, this);
    }
}
