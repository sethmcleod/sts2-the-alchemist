using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Ancient;

[CardTheme(CardTheme.Poison, CardTheme.Antitoxin)]
public class Panacea : AlchemistCard
{
    public Panacea() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PanaceaPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
