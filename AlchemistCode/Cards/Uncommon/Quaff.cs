using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The valve: the bigger the dose you are carrying, the more this refills. Tolerance's shape at
// Uncommon, without the limit increase
[CardTheme(CardTheme.Antitoxin, CardTheme.Poison)]
public class Quaff : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Quaff() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("antitoxin", 2, 1);
        WithTip(typeof(AntitoxinPower));
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Dose", Dose(this) is var p and > 0 ? $" ([green]{p}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["antitoxin"].IntValue + (int)Dose(this);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, total, Owner.Creature, this);
    }
}
