using Alchemist.AlchemistCode.Cards.Rare;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Citrinitas : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Exhaust;

    public Citrinitas() : base(1, CardType.Attack, CardRarity.Token, TargetType.AllEnemies)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(RegenPower));
        WithUpgradingCardTip<Rubedo>();
        WithUpgradingCardTip<Nigredo>();
        WithUpgradingCardTip<Albedo>();
    }

    private int DamagePer => Owner?.Creature is { } c ? c.GetPowerAmount<RegenPower>() : 0;

    protected override int? RawFormulaDamagePreview => DamagePer > 0 ? DamagePer : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var hits = ReactionActive ? 2 : 1;
        var perHit = ApplyEnchantDamage(DamagePer);
        if (perHit > 0)
            await DamageCmd.Attack(perHit)
                .WithHitCount(hits)
                .WithHitFx(HitVfx("vfx/vfx_starry_impact"))
                .FromCard(this, play)
                .TargetingAllOpponents(CombatState)
                .Execute(choiceContext);
        await AlchemistCardCmd.GiveCard<Rubedo>(this);
    }
}
