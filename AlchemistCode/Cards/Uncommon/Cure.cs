using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Decant, CardTheme.Antitoxin)]
public class Cure : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Cure() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(6, 3);
        WithVar("DecantMax", 2, -1);
        WithVar("antitoxin", 4, 0);
        WithCards(1, 0);
        WithTip(typeof(AntitoxinPower));
    }

    protected override bool Decants => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (!TrySpendDecant()) return;
        await CommonActions.ApplySelf<AntitoxinPower>(choiceContext, this,
            DynamicVars["antitoxin"].IntValue);
        await CommonActions.Draw(this, choiceContext);
    }
}
