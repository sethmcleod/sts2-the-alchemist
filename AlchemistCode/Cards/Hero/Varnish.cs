using System.Linq;
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
// enchantments last the run like the base game's. This is the Alchemist's. Laced keys on
// IsPoweredAttack, so a card that deals its damage Unpowered would carry a promise that never fires
[CardTheme(CardTheme.Poison)]
public class Varnish : AlchemistHeroCard
{
    private const int LacedAmount = 1;

    private static LocString Prompt => new("cards", "ALCHEMIST-VARNISH.selectionScreenPrompt");

    public Varnish() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithTips(_ => HoverTipFactory.FromEnchantment<Laced>(LacedAmount));
    }

    private bool CanLace(CardModel card) =>
        card != this && card.Type == CardType.Attack && card.Enchantment == null
        && card is not AlchemistCard { DealsUnpoweredDamage: true }
        && ModelDb.Enchantment<Laced>().CanEnchant(card);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        if (!PileType.Hand.GetPile(Owner).Cards.Any(CanLace)) return;
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(Prompt, 1), CanLace, this)).FirstOrDefault();
        if (chosen != null) CardCmd.Enchant<Laced>(chosen, LacedAmount);
    }
}
