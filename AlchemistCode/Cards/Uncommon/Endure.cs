using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Endure : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Endure() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(7, 3);
        WithVar("antitoxin", 3, 1);
        WithCards(1);
        WithTip(typeof(AntitoxinPower));
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this, DynamicVars["antitoxin"].IntValue);
        if (Dose(this) > 0) await CommonActions.Draw(this, choiceContext);
    }
}
