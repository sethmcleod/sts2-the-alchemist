using System.Linq;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class FreeSamples : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    public FreeSamples() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Infusions", 2, 1);
        WithCards(2, 0);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var count = DynamicVars["Infusions"].IntValue;
        foreach (var ally in CombatState.Players.Where(p => p != Owner && p.Creature is { IsAlive: true }))
            Infusion.InfuseRandomFromHand(ally, count);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
