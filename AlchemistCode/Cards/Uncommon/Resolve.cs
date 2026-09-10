using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class Resolve : AlchemistCard
{
    protected override bool Ferments => true;

    public Resolve() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar(new FermentVar("Amount", 1, "perTurn"));
        WithVar("perTurn", 1, 1);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(StrengthPower));
    }

    private int Resolved => ((FermentVar)DynamicVars["Amount"]).Total(this, null);


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, Resolved, Owner.Creature, this);
    }
}
