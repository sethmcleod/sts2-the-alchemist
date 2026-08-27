using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.None)]
public class Brace : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Brace() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(5, 3);
        WithTip(typeof(WeakPower));
        WithTip(typeof(FrailPower));
    }

    private bool Debuffed =>
        IsMutable && Owner != null
        && (Owner.Creature.HasPower<WeakPower>() || Owner.Creature.HasPower<FrailPower>());

    protected override bool ConditionalGlow => Debuffed;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (Owner.Creature.HasPower<WeakPower>())
            await PowerCmd.Remove<WeakPower>(Owner.Creature);
        if (Owner.Creature.HasPower<FrailPower>())
            await PowerCmd.Remove<FrailPower>(Owner.Creature);
    }
}
