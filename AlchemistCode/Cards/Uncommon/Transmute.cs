using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Transmute : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Transmute() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(StrengthPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poison > 0)
            await PowerCmd.Apply<TransmuteStrengthPower>(choiceContext, Owner.Creature, poison, Owner.Creature, this);

        if (IsReduced)
        {
            var rarity = IsUpgraded ? PotionRarity.Uncommon : PotionRarity.Common;
            var rng = Owner.RunState.Rng.CombatPotionGeneration;
            var options = PotionFactory.GetPotionOptions(Owner)
                .Where(p => p.CanBeGeneratedInCombat && p.Rarity == rarity)
                .ToList();
            var potion = rng.NextItem(options);
            if (potion != null)
                await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }
}
