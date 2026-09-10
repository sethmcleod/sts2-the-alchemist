using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Linq;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Ferment)]
public class Uncork : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Uncork() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(1, 0);
        WithVar(new EnergyVar("Bonus", 1).WithUpgrade(1));
        WithKeyword(CardKeyword.Exhaust);
        WithTips(_ => new[] { AlchemistTips.FermentRef });
    }

    private IEnumerable<AlchemistCard> Brewing =>
        !IsMutable || Owner == null
            ? Enumerable.Empty<AlchemistCard>()
            : PileType.Hand.GetPile(Owner).Cards.OfType<AlchemistCard>().Where(c => c.IsFermentInline);

    protected override bool ConditionalGlow => Brewing.Any();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var brewing = Brewing.ToList();
        foreach (var card in brewing)
            await card.AdvanceFerment(1);
        if (brewing.Count > 0) CardCmd.Preview(brewing.Cast<CardModel>().ToList());
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue + DynamicVars["Bonus"].IntValue * brewing.Count, Owner);
    }
}
