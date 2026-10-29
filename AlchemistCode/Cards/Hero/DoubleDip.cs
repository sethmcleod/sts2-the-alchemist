using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Alchemist.AlchemistCode.Cards.Hero;

[CardTheme(CardTheme.Poison)]
public class DoubleDip : AlchemistHeroCard
{
    private static readonly Color Tint = new("83eb85");

    protected internal override bool PlaysCastAnimation => false;

    public DoubleDip() : base(2, CardType.Skill, CardRarity.Common, TargetType.RandomEnemy)
    {
        WithPower<PoisonPower>(4, 1);
        WithVar(new RepeatVar(2));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var from = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.VfxSpawnPosition;
        for (var i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            if (Owner.RunState.Rng.CombatTargets.NextItem(combat.HittableEnemies) is not { } enemy) continue;
            from = await Throw(from, enemy);
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, DynamicVars.Poison.IntValue, Owner.Creature, this);
        }
    }

    private static async Task<Vector2?> Throw(Vector2? from, Creature enemy)
    {
        if (NCombatRoom.Instance is not { } room || room.GetCreatureNode(enemy) is not { } node || from is not { } start)
            return from;
        room.CombatVfxContainer.AddChildSafely(
            NItemThrowVfx.Create(start, node.GetBottomOfHitbox(), ModelDb.Potion<PoisonPotion>().Image));
        await Cmd.Wait(0.5f);
        room.CombatVfxContainer.AddChildSafely(NSplashVfx.Create(node.VfxSpawnPosition, Tint));
        room.CombatVfxContainer.AddChildSafely(NLiquidOverlayVfx.Create(enemy, Tint));
        room.CombatVfxContainer.AddChildSafely(NGaseousImpactVfx.Create(node.VfxSpawnPosition, Tint));
        return node.VfxSpawnPosition;
    }
}
