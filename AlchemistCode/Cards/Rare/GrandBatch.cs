using System.Linq;
using Alchemist.AlchemistCode.Cards.Token;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Mix)]
public class GrandBatch : AlchemistCard
{
    public GrandBatch() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTips(card => Mixing.MixTips(card.IsUpgraded));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        foreach (var kind in Mixing.All)
        {
            var mix = Mixing.Create(combat, Owner, kind);
            if (IsUpgraded) CardCmd.Upgrade(mix);
            Mixing.RecordCreated(Owner, mix);
            await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
        }
    }
}
