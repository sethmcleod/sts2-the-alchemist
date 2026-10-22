using System.Linq;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Enchantments;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Hero;

// The Hero Expansion gives every character one Common that Enchants a card mid-combat, and its
// enchantments last the run like the base game's. This is the Alchemist's. The enchantment itself
// refuses a card that already prints the Laced keyword
[CardTheme(CardTheme.Poison)]
public class SecretSauce : AlchemistHeroCard
{
    private const int LacedAmount = 1;

    private static LocString Prompt => new("cards", "ALCHEMIST-SECRET_SAUCE.selectionScreenPrompt");

    public SecretSauce() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithUpgradingCardTip<Token.BurstingMix>();
        WithUpgradingCardTip<Token.SyrupyMix>();
        WithUpgradingCardTip<Token.ZestyMix>();
        WithTips(_ => HoverTipFactory.FromEnchantment<Laced>(LacedAmount));
    }

    private bool CanLace(CardModel card) =>
        card != this && ModelDb.Enchantment<Laced>().CanEnchant(card);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var mix = await Mixing.Choose(choiceContext, Owner, upgraded: IsUpgraded, source: this);
        if (mix != null) await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
        if (!PileType.Hand.GetPile(Owner).Cards.Any(CanLace)) return;
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(Prompt, 1), CanLace, this)).FirstOrDefault();
        if (chosen != null) CardCmd.Enchant<Laced>(chosen, LacedAmount);
    }
}
