using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison, CardTheme.Antitoxin)]
public class Vent : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Vent() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithCalculatedVar("Antitoxin", 0, static (card, _) => Dose(card));
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var dose = (int)Dose(this);
        if (dose <= 0) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, dose, Owner.Creature, this);
    }
}
