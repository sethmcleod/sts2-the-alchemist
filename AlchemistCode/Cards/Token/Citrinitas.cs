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
    public Citrinitas() : base(1, CardType.Attack, CardRarity.Token, TargetType.Self)
    {
        WithVar("Hits", 2, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Rubedo>();
    }

    // Per-hit damage = your Regen (after enchant multipliers) — shared by preview and the real hit
    private int DamageFor(int regen) => ApplyEnchantDamage(regen);

    protected override int? FormulaDamagePreview =>
        Owner?.Creature is { } c && c.GetPowerAmount<RegenPower>() is var r and > 0
            ? DamageFor(r) : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;

        var regenAmount = Owner.Creature.GetPowerAmount<RegenPower>();
        if (regenAmount > 0)
            await PowerCmd.Remove<RegenPower>(Owner.Creature);

        var perHit = DamageFor(regenAmount);
        if (perHit > 0)
            await DamageCmd.Attack(perHit)
                .WithHitCount(DynamicVars["Hits"].IntValue)
                .FromCard(this, play)
                .TargetingAllOpponents(CombatState)
                .Execute(choiceContext);

        await AlchemistCardCmd.GiveCard<Rubedo>(this);
    }
}
