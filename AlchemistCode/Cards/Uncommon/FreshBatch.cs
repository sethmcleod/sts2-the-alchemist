using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class FreshBatch : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public FreshBatch() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("GambitBlock", 10, 4);
        WithTip(StaticHoverTip.Block);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var gambit = IsReduced;
        await Procure();
        // Block rather than more potions: at a third HP the card has to keep you alive, and the extra
        // potions were feeding an already over-tuned economy
        if (gambit)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["GambitBlock"].IntValue,
                ValueProp.Move, null);
    }

    private Task Procure() =>
        PotionCmd.TryToProcure(
            PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
            Owner);
}
