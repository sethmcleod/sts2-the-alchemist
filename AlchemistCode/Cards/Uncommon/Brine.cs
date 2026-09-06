using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Poison)]
public class Brine : AlchemistCard
{
    protected override bool Ferments => true;
    protected internal override bool PlaysCastAnimation => false;

    public Brine() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Poison", 2, 1);
        WithVar("perTurn", 1, 0);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    private int Soaked => DynamicVars["Poison"].IntValue + DynamicVars["perTurn"].IntValue * FermentTurns;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Soaked", FermentTurns > 0 ? $" ([green]{Soaked}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<BrinePower>(choiceContext, Owner.Creature, Soaked, Owner.Creature, this);
    }
}
