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

[CardTheme(CardTheme.Antitoxin, CardTheme.Mix)]
public class Digest : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Digest() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<AntitoxinPower>(1, 1);
        WithTips(_ => Mixing.MixTips());
    }

    private static bool IsJunk(CardModel card) =>
        card.Type is CardType.Status or CardType.Curse;

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && PileType.Hand.GetPile(Owner).Cards.Any(IsJunk);

    private static LocString EatPrompt => new("cards", "ALCHEMIST-DIGEST.selectionScreenPrompt");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this);
        // No screen at all when there is nothing to eat, and the pick is skippable (min 0)
        if (!PileType.Hand.GetPile(Owner).Cards.Any(IsJunk)) return;
        var eaten = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(EatPrompt, 0, 1),
            filter: IsJunk, source: this)).FirstOrDefault();
        if (eaten == null) return;
        await CardCmd.Exhaust(choiceContext, eaten);
        await Mixing.CreateRandom(choiceContext, Owner);
    }
}
