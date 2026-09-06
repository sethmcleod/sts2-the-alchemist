using System.Linq;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Mix)]
public class Wring : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Wring() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Per", 2, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(card => new[] { AlchemistTips.FermentRef }.Concat(Mixing.MixTips(card.IsUpgraded)));
    }

    private bool Pays(CardModel card) =>
        card is AlchemistCard { IsFermentInline: true } ferment && ferment.FermentTurns >= DynamicVars["Per"].IntValue;

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && PileType.Hand.GetPile(Owner).Cards.Any(Pays);

    private static LocString Prompt => new("cards", "ALCHEMIST-WRING.selectionScreenPrompt");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!PileType.Hand.GetPile(Owner).Cards.Any(IsBrewing)) return;
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(Prompt, 1), filter: IsBrewing, source: this)).FirstOrDefault();
        if (chosen is not AlchemistCard ferment) return;
        var mixes = ferment.FermentTurns / DynamicVars["Per"].IntValue;
        for (var i = 0; i < mixes; i++)
            await Mixing.CreateRandom(choiceContext, Owner, IsUpgraded);
    }
}
