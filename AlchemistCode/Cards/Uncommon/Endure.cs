using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Endure : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Endure() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(6, 3);
        WithVar("antitoxin", 3, 0);
        WithCards(1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this, DynamicVars["antitoxin"].IntValue);
        await CommonActions.Draw(this, choiceContext);
    }
}
