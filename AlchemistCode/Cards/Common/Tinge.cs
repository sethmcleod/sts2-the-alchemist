using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Tinge : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public Tinge() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(3, 1);
        WithPower<PoisonPower>(2, 0);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Seep) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        if (CombatState == null) return;
        var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;
        var target = Owner.RunState.Rng.CombatTargets.NextItem(enemies)!;
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, 3, Owner.Creature, this);
    }
}
