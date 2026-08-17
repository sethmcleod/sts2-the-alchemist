using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class CausticStrike : AlchemistCard
{
    public CausticStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // A "Strike" card, so base-game strike synergies such as Perfected Strike count it. Patient
        // Strike was the only draftable one
        WithTags(CardTag.Strike);
        WithDamage(6, 3);
        WithVar("antitoxin", 2, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
