using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.None)]
public class Preserve : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Preserve() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithVar("Turns", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target is not { IsAlive: true } ally || ally == Owner.Creature) return;
        await PowerCmd.Apply<BlurPower>(choiceContext, ally, DynamicVars["Turns"].IntValue, Owner.Creature, this);
    }
}
