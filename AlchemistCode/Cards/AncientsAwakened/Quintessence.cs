using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

// Sebastian's Experimental Serum hands this over, always upgraded. Panacea and Wormwood cover the
// Poison and Antitoxin processes, so this one bridges the other two
[CardTheme(CardTheme.Ferment, CardTheme.Mix)]
public class Quintessence : AlchemistAncientsCard
{
    public Quintessence() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
        WithTips(_ => new[] { AlchemistTips.FermentRef });
        WithTips(card => Mixing.MixTips(card.IsUpgraded));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var power = await PowerCmd.Apply<QuintessencePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && IsUpgraded) power.Plus = true;
    }
}
