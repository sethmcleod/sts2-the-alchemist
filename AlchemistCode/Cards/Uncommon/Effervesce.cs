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
        WithTips(card => AlchemistTips.MixTrio(card.IsUpgraded));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target?.Player is not { } targetPlayer) return;
        foreach (var mix in new CardModel[]
                 {
                     CombatState.CreateCard<BurstingMix>(targetPlayer),
                     CombatState.CreateCard<SyrupyMix>(targetPlayer),
                     CombatState.CreateCard<FumingMix>(targetPlayer),
                 })
        {
            if (IsUpgraded) CardCmd.Upgrade(mix);
            Mixing.RecordCreated(Owner, mix);
            await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, targetPlayer);
        }
    }
}
