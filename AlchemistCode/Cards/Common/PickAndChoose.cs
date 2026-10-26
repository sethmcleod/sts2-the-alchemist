using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Antitoxin)]
public class PickAndChoose : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public PickAndChoose() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar(new ScryVar(2).WithUpgrade(1));
        WithCards(1);
        WithVar("antitoxin", 2, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Scrying.Execute(choiceContext, this);
        await CommonActions.Draw(this, choiceContext);
        await CardCmd.Discard(choiceContext, await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, DynamicVars.Cards.IntValue), null, this));
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this, DynamicVars["antitoxin"].IntValue);
    }
}
