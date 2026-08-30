using System.Linq;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Mix)]
public class Reclaim : AlchemistCard
{
    public Reclaim() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => Mixing.MixTips());
    }

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null
        && PileType.Exhaust.GetPile(Owner).Cards.Any(Mixing.IsMix);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        // Snapshot before moving: the move mutates the pile the query reads
        var mixes = PileType.Exhaust.GetPile(Owner).Cards.Where(Mixing.IsMix).ToList();
        foreach (var mix in mixes)
            await CardPileCmd.Add(mix, PileType.Draw, CardPilePosition.Random);
    }
}
