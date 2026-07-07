using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Corrode : AlchemistCard
{
    protected override bool IsGambitCard => true;
    protected override bool IsSeepCard => true;

    public Corrode() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<PoisonPower>(6, 0);
        WithPower<WeakPower>(1, 1);
        WithVar("Regen", 2, 0);
        WithVar("SelfPoison", 3, 0);
        WithTip(typeof(RegenPower));
        WithTips(_ => new[]
        {
            HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit),
            HoverTipFactory.FromKeyword(AlchemistKeywords.Seep)
        });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, DynamicVars.Poison.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
        if (IsReduced)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
                DynamicVars["Regen"].BaseValue, Owner.Creature, this);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].BaseValue, Owner.Creature, this);
    }
}
