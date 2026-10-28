using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Quench : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Quench() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("antitoxin", 6, 3);
        WithKeyword(CardKeyword.Exhaust);
        WithCalculatedBlock(0, static (card, _) => BlockFrom(card), ValueProp.Move);
        WithTip(typeof(AntitoxinPower));
    }

    private static decimal BlockFrom(CardModel card) =>
        card is Quench { IsMutable: true, Owner.Creature: { } creature, CombatState: { } combat } quench
            ? creature.GetPowerAmount<AntitoxinPower>() + Grant(combat, creature, quench)
            : 0m;

    private static decimal Grant(ICombatState combat, Creature creature, Quench quench)
    {
        var power = ModelDb.Power<AntitoxinPower>();
        var given = Hook.ModifyPowerAmountGiven(combat, power, creature, quench.DynamicVars["antitoxin"].IntValue,
            creature, quench, out _);
        return Hook.ModifyPowerAmountReceived(combat, power, creature, given, creature, out _);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, AntitoxinCapacity, ValueProp.Move, play);
    }
}
