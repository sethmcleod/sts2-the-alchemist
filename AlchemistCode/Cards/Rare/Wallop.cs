using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.None)]
public class Wallop : AlchemistCard
{
    public Wallop() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) =>
                (card.IsUpgraded ? 4m : 3m) * PlayedThisTurn(card),
            ValueProp.Move, 2, 0);
    }

    // CardPlaysFinished, so the play in progress is not in the history yet and Wallop never counts
    // itself. HappenedThisTurn is the base game's own filter, which stays right across an extra turn
    private static int PlayedThisTurn(CardModel card) =>
        card.IsMutable && card.Owner?.Creature.CombatState is { } combat
            ? CombatManager.Instance?.History.CardPlaysFinished
                .Count(e => e.HappenedThisTurn(combat) && e.CardPlay.Card.Owner == card.Owner) ?? 0
            : 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"),
            tmpSfx: "heavy_attack.mp3").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
