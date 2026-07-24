using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class FullMeasure : AlchemistCard
{
    public FullMeasure() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(7, 3, static (card, _) =>
            PileType.Hand.GetPile(((AlchemistCard)card).Owner).Cards.Count(c => c.Enchantment != null),
            ValueProp.Move, 4, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_blunt"), tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
    }
}
