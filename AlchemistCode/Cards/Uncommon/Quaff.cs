using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin, CardTheme.Poison)]
public class Quaff : AlchemistCard
{
    // Poison per point of capacity granted. One-shot, so it reads the dose harder than Tolerance
    private const int PoisonPerPoint = 2;

    protected internal override bool PlaysCastAnimation => false;

    public Quaff() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("antitoxin", 1, 1);
        WithVar("per", PoisonPerPoint, 0);
        WithTip(typeof(AntitoxinPower));
        WithTip(typeof(PoisonPower));
    }

    private int FromDose => (int)Dose(this) / PoisonPerPoint;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Dose", FromDose > 0 ? $" ([green]{FromDose}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["antitoxin"].IntValue + FromDose;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, total, Owner.Creature, this);
    }
}
