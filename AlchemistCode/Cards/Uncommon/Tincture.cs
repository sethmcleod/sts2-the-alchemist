using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Tincture : AlchemistCard
{
    public Tincture() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("flat", 2, 0);
        WithVar("each", 3, 1);
        WithTip(typeof(AntitoxinPower));
    }

    private int Potions => IsMutable && CombatState != null ? Owner.Potions.Count() : 0;

    protected override bool ConditionalGlow => Potions > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["flat"].IntValue + DynamicVars["each"].IntValue * Owner.Potions.Count();
        if (total <= 0) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, total, Owner.Creature, this);
    }
}
