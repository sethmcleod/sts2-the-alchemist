using Alchemist.AlchemistCode.Compat;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Transmute : AlchemistCard
{
    public Transmute() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithCards(1, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {

        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poison > 0)
            await PowerCmd.Apply<TransmuteStrengthPower>(choiceContext, Owner.Creature, poison, Owner.Creature, this);

        await CommonActions.Draw(this, choiceContext);
    }
}
