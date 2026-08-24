using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Ferment, CardTheme.Antitoxin)]
public class Distill : AlchemistCard
{
    protected override bool Ferments => true;

    protected internal override bool PlaysCastAnimation => false;

    public Distill() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("SelfPoison", 2, 0);
        WithVar("Antitoxin", 2, 1);
        WithVar("perTurn", 1, 0);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    private int Distilled => DynamicVars["Antitoxin"].IntValue
        + DynamicVars["perTurn"].IntValue * FermentTurns;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Distilled", FermentTurns > 0 ? $" ([green]{Distilled}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, Distilled, Owner.Creature, this);
    }
}
