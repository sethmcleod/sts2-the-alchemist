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
    protected override ReactionCondition Reaction => ReactionCondition.Exhaust;

    public Transmute() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithCards(1, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Read the Reaction before this card's own Exhaust lands, so it cannot satisfy its own condition
        var reacted = ReactionActive;

        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poison > 0)
            await PowerCmd.Apply<TransmuteStrengthPower>(choiceContext, Owner.Creature, poison, Owner.Creature, this);

        if (reacted)
            await CommonActions.Draw(this, choiceContext);
    }
}
