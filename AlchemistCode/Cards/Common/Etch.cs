using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Etch : AlchemistCard
{
    public Etch() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // Move keeps Strength and Vulnerable; only enemy Block is ignored
        WithVar(new DamageVar(14, ValueProp.Move | ValueProp.Unblockable).WithUpgrade(4));
        WithTip(StaticHoverTip.Block);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
    }
}
