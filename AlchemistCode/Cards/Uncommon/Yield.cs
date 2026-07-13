using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Yield : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Yield() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Extra", 1, 1);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Procure();
        if (IsReduced)
            for (var i = 0; i < DynamicVars["Extra"].IntValue; i++)
                await Procure();
    }

    private Task Procure() =>
        PotionCmd.TryToProcure(
            PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
            Owner);
}
