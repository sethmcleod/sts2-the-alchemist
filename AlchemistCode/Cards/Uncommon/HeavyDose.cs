using MegaCrit.Sts2.Core.HoverTips;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison, CardTheme.Mix)]
public class HeavyDose : AlchemistCard
{
    public HeavyDose() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(24, 6);
        WithVar("Per", 4, 0);
        WithTips(_ => new[] { HoverTipFactory.FromCard<Token.BurstingMix>() });
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) >= DynamicVars["Per"].IntValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"),
            tmpSfx: "heavy_attack.mp3")
            .WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay).Execute(choiceContext);
        var mixes = Owner.Creature.GetPowerAmount<PoisonPower>() / DynamicVars["Per"].IntValue;
        for (var i = 0; i < mixes; i++)
            await Mixing.CreateOne<Token.BurstingMix>(choiceContext, Owner, source: this);
    }
}
