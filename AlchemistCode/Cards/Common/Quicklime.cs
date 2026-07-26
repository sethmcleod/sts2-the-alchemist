using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Quicklime : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public Quicklime() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 2);
        WithBlock(5, 2);
        WithVar("SeepBlock", 3, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
        await CommonActions.CardBlock(this, play);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["SeepBlock"].BaseValue, ValueProp.Move, null);
    }
}
