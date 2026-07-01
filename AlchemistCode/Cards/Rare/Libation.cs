using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Libation : AlchemistCard
{
    public Libation() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("block", 4, 2);
        WithVar("poison", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var potions = Owner.Potions.ToList();
        foreach (var potion in potions)
        {
            await PotionCmd.Discard(potion);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["block"].BaseValue, ValueProp.Move, play);
            foreach (var enemy in CombatState!.Enemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, DynamicVars["poison"].BaseValue, Owner.Creature, this);
        }
    }
}
