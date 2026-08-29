using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.None)]
public class FlareUp : AlchemistCard
{
    public FlareUp() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) });
    }

    private static LocString FuelPrompt => new("cards", "ALCHEMIST-FLARE_UP.selectionScreenPrompt");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Fuel first, so the exhaust visibly feeds the flare before it lands
        CardModel? fuel = null;
        if (PileType.Hand.GetPile(Owner).Cards.Count > 0)
            fuel = (await CardSelectCmd.FromHand(choiceContext, Owner,
                new CardSelectorPrefs(FuelPrompt, 0, 1), filter: null, source: this)).FirstOrDefault();
        var hits = 1;
        if (fuel != null)
        {
            await CardCmd.Exhaust(choiceContext, fuel);
            hits = 2;
        }
        await CommonActions.CardAttack(this, play, hitCount: hits, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
    }
}
