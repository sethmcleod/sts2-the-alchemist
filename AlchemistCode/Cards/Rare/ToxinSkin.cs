using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Poison)]
public class ToxinSkin : AlchemistCard
{
    public ToxinSkin() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithVar("poison", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ToxinSkinPower>(choiceContext, Owner.Creature,
            DynamicVars["poison"].IntValue, Owner.Creature, this);
    }
}
