using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class Effervesce : AlchemistCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public Effervesce() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithUpgradingCardTip<BurstingMix>();
        WithUpgradingCardTip<SyrupyMix>();
        WithUpgradingCardTip<ZestyMix>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target?.Player is not { } targetPlayer) return;
        foreach (var kind in Mixing.Basic)
        {
            var mix = Mixing.Create(CombatState, targetPlayer, kind);
            if (IsUpgraded) CardCmd.Upgrade(mix);
            Mixing.RecordCreated(Owner, mix, this);
            await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, targetPlayer);
        }
    }
}
