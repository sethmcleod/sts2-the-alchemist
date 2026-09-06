using System.Linq;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Combine : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Combine() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithUpgradingCardTip<Token.BurstingMix>();
        WithUpgradingCardTip<Token.SyrupyMix>();
        WithUpgradingCardTip<Token.ZestyMix>();
        WithTips(_ => new[] { AlchemistTips.CompoundMix });
    }

    private static LocString Prompt => new("cards", "ALCHEMIST-COMBINE.selectionScreenPrompt");

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && PileType.Hand.GetPile(Owner).Cards.Any(Mixing.IsIngredient);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        await Mixing.CreateRandom(choiceContext, Owner, IsUpgraded, Mixing.Basic, this);
        var mixes = PileType.Hand.GetPile(Owner).Cards.Where(Mixing.IsIngredient).ToList();
        if (mixes.Count < 2) return;
        // Exactly two skips the screen; more asks which two
        var picked = mixes.Count == 2
            ? mixes
            : (await CardSelectCmd.FromHand(choiceContext, Owner, new CardSelectorPrefs(Prompt, 2, 2),
                Mixing.IsIngredient, this)).ToList();
        if (picked.Count < 2) return;
        foreach (var mix in picked)
            await CardCmd.Exhaust(choiceContext, mix);
        await Mixing.CreateCompound(choiceContext, Owner, picked[0], picked[1], this);
    }
}
