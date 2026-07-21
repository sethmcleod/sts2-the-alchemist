using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Masterwork : AlchemistCard
{
    private const int EnchantThreshold = 5;

    public Masterwork() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithEnergy(1, 1);
        WithVar("Cards", 1, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    // The play itself infuses one card, so the glow turns on one below the threshold: the play that
    // starts now can reach 7. OnPlay still checks the real count after the infusion, because an infuse
    // of an already Enchanted card adds no new card
    protected override bool ConditionalGlow =>
        Infusion.EnchantedThisCombatCount(Owner) >= EnchantThreshold - 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
        if (Owner == null || Infusion.EnchantedThisCombatCount(Owner) < EnchantThreshold)
            return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, Owner);
        ExhaustOnNextPlay = true;
    }
}
