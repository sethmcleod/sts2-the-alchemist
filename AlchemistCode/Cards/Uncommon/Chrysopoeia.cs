using Alchemist.AlchemistCode;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Chrysopoeia : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Chrysopoeia() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("gold", 2, 1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ChrysopoeiaPower>(choiceContext, Owner.Creature,
            DynamicVars["gold"].IntValue, Owner.Creature, this);
        if (IsReduced)
            await CreatureCmd.Heal(Owner.Creature, 3);
    }
}
