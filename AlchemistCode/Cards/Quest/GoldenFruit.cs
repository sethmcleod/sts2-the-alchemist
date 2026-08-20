using Alchemist.AlchemistCode.Compat;
using Alchemist.AlchemistCode.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Quest;

[Pool(typeof(QuestCardPool))]
[CardTheme(CardTheme.None)]
public class GoldenFruit : AlchemistCard
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<AlchemistCardPool>();

    public GoldenFruit() : base(1, CardType.Skill, CardRarity.Quest, TargetType.Self)
    {
        WithVar("gold", 25);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainGold(DynamicVars["gold"].BaseValue, Owner);
        await ExtraTurn.Grant(choiceContext, Owner.Creature, this);
    }
}
