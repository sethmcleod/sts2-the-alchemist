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

// QuestCardPool keeps this out of the Alchemist card list and files it under Quest, the same as the base
// game's own quest cards
[Pool(typeof(QuestCardPool))]
public class GoldenFruit : AlchemistCard
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    // Only Midas Fruit creates this, so it wears the Alchemist frame. VisualCardPool sets the look without
    // touching the real pool that keeps it off the card list, as the base game does for Trash Heap cards
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<AlchemistCardPool>();

    public GoldenFruit() : base(1, CardType.Skill, CardRarity.Quest, TargetType.Self)
    {
        WithVar("gold", 25);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainGold(DynamicVars["gold"].BaseValue, Owner);
        // The base game's invisible extra-turn counter
        await ExtraTurn.Grant(choiceContext, Owner.Creature, this);
    }
}
