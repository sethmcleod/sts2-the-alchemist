using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Circulation : AlchemistCard
{
    public Circulation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Regen", 2, 2);
        WithTip(typeof(RegenPower));
    }

    // Half of the Regen you will hold after this card grants its own, rounded down. The game truncates
    // fractional HP the same way, so a round number of cards is what the player expects
    private int DrawCount
    {
        get
        {
            if (!IsMutable || CombatState == null) return 0;
            var after = Owner.Creature.GetPowerAmount<RegenPower>() + DynamicVars["Regen"].IntValue;
            return after / 2;
        }
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("DrawLine",
            DrawCount is var n and > 0 ? $"\n(Draw [green]{n}[/green] {(n == 1 ? "card" : "cards")}.)" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Gain first, so the card always has something to spend even on an empty board
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
            DynamicVars["Regen"].IntValue, Owner.Creature, this);
        var half = Owner.Creature.GetPowerAmount<RegenPower>() / 2;
        if (half <= 0) return;
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, -half, Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, half, Owner);
    }
}
