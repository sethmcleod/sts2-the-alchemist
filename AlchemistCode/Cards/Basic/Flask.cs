using Alchemist.AlchemistCode.Potions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Flask : AlchemistCard
{
    private bool _playedThisCombat;

    protected override bool ConditionalGlow => CombatState != null && !_playedThisCombat;

    public Flask() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(4, 3);
        WithTips(_ => new[] { UnstablePotions.Tip });
    }

    public override Task BeforeCombatStart()
    {
        _playedThisCombat = false;
        return base.BeforeCombatStart();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (_playedThisCombat) return;

        _playedThisCombat = true;
        var potion = PotionFactory
            .CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration)
            .ToMutable();
        var result = await PotionCmd.TryToProcure(potion, Owner);
        if (result.success)
            UnstablePotions.Mark(result.potion);
    }
}
