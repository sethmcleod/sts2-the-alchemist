using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Vitrify : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

public Vitrify() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(3, 1);
        WithTip(typeof(AntitoxinPower));
    }

    internal override bool GainsEffectWhenEnchanted => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.CardBlock(this, play);
        if (IsEnchanted)
            await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, 3, Owner.Creature, this);
    }
}
