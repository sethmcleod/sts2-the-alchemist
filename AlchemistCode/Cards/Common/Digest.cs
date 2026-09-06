using System.Linq;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Digest : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Digest() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithTips(card => Mixing.MixTips(card.IsUpgraded));
    }

    private static bool IsJunk(CardModel card) =>
        card.Type is CardType.Status or CardType.Curse;

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && PileType.Hand.GetPile(Owner).Cards.Any(IsJunk);

    private static LocString EatPrompt => new("cards", "ALCHEMIST-DIGEST.selectionScreenPrompt");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var eaten = PileType.Hand.GetPile(Owner).Cards.Count == 0
            ? null
            : (await CardSelectCmd.FromHand(choiceContext, Owner,
                new CardSelectorPrefs(EatPrompt, 1), filter: null, source: this)).FirstOrDefault();
        if (eaten != null)
            await CardCmd.Exhaust(choiceContext, eaten);
        if (eaten != null && IsJunk(eaten))
            await Mixing.CreateRandom(choiceContext, Owner, IsUpgraded);
    }
}
