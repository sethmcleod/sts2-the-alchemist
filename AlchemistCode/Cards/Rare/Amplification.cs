using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Amplification : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Amplification() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithTip(typeof(PoisonPower));
        WithTip(typeof(RegenPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var multiplier = IsUpgraded ? 2 : 1;
        var allCreatures = CombatState!.Enemies.Where(e => e.IsAlive)
            .Append(Owner.Creature);
        foreach (var creature in allCreatures)
        {
            var poison = creature.GetPowerAmount<PoisonPower>();
            if (poison > 0)
                await PowerCmd.Apply<PoisonPower>(choiceContext, creature, poison * multiplier, Owner.Creature, this);
            var regen = creature.GetPowerAmount<RegenPower>();
            if (regen > 0)
                await PowerCmd.Apply<RegenPower>(choiceContext, creature, regen * multiplier, Owner.Creature, this);
        }
        if (IsReduced)
            await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
