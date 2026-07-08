using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class MercurialForm : AlchemistCard
{
    public MercurialForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Strength", 1, 1);
        WithTip(typeof(StrengthPower));
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<MercurialFormPower>(choiceContext, Owner.Creature,
            DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }
}
