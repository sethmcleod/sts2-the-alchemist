using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

// The pot everything goes into: the only card that reads the TOTAL fermentation held in the
// hand, so a wide ferment board cashes into one hit
[CardTheme(CardTheme.Ferment)]
public class RollingBoil : AlchemistCard
{
    protected override bool Ferments => true;

    public RollingBoil() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(8, static (card, _) =>
                card is RollingBoil { IsMutable: true, Owner: { } owner } boil
                    ? (card.IsUpgraded ? 4m : 3m) * boil.TotalFermentation(owner)
                    : 0m,
            ValueProp.Move, 2, 0);
        WithKeyword(CardKeyword.Retain);
    }

    // Own turns counted directly, so the previewed number does not dip when the card leaves
    // the hand for the play zone; every other Ferment card is read from the hand as the text says
    private int TotalFermentation(Player owner) =>
        FermentTurns + PileType.Hand.GetPile(owner).Cards
            .OfType<AlchemistCard>()
            .Where(c => c != this && c.IsFermentInline)
            .Sum(c => c.FermentTurns);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
