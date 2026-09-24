using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison, CardTheme.Mix)]
public class HeavyDose : AlchemistCard
{
    public HeavyDose() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(28, 8);
        WithVar("SelfPoison", 4, 0);
        WithVar("Per", 4, 0);
        WithUpgradingCardTip<Token.BurstingMix>();
        WithTip(typeof(PoisonPower));
    }

    // Glows when the dose already held makes a second Mix; the card's own gain always makes the first
    protected override bool ConditionalGlow => Dose(this) >= DynamicVars["Per"].IntValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"),
            tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay).Execute(choiceContext);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
        var mixes = (int)Dose(this) / DynamicVars["Per"].IntValue;
        for (var i = 0; i < mixes; i++)
            await Mixing.CreateOne<Token.BurstingMix>(choiceContext, Owner, IsUpgraded, this);
    }
}
