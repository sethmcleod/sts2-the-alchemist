using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Transform, CardTheme.Mix)]
public class Blend : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Blend() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(5, 3);
        WithTip(StaticHoverTip.Transform);
        WithTips(_ => Mixing.MixTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (CombatState == null) return;
        var discard = PileType.Discard.GetPile(Owner);
        if (discard.Cards.Count == 0) return;
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, discard, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1))).FirstOrDefault();
        if (chosen == null) return;
        var mix = await Mixing.TransformIntoChosen(choiceContext, Owner, chosen);
        if (mix == null) return;
        await CardPileCmd.Add(mix, PileType.Hand);
    }
}
