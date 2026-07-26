using System.Linq;
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
        ExplainNumber("ALCHEMIST-MASTERWORK");
    }

    // The play itself infuses one card, so the glow turns on one below the threshold, but only when a card
    // in hand can take a NEW enchant. With none, for example Masterwork alone in hand, the play cannot
    // raise the count. OnPlay rechecks the real count anyway, since infusing an Enchanted card adds nothing
    protected override bool ConditionalGlow
    {
        get
        {
            if (Owner == null) return false;
            var count = Infusion.EnchantedThisCombatCount(Owner);
            if (count >= EnchantThreshold) return true;
            return count == EnchantThreshold - 1
                && PileType.Hand.GetPile(Owner).Cards.Any(c => c != this && Infusion.WouldNewlyEnchant(c));
        }
    }

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
