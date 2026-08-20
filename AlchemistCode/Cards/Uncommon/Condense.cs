using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Transform, CardTheme.Mix)]
public class Condense : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Condense() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("cards", 1, 1);
        WithTip(StaticHoverTip.Transform);
        WithTips(_ => Mixing.MixTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var selected = await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Draw.GetPile(Owner), Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, DynamicVars["cards"].IntValue));
        foreach (var card in selected)
        {
            var mix = await Mixing.Choose(choiceContext, Owner);
            if (mix == null) return;
            await CardCmd.Transform(card, mix);
            await CardPileCmd.Add(mix, PileType.Hand);
        }
    }
}
