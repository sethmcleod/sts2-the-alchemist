using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Catharsis : AlchemistCard
{
    public Catharsis() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<RegenPower>(3, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(WeakPower));
        WithTip(typeof(VulnerablePower));
        WithTip(typeof(FrailPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
        if (Owner.Creature.HasPower<WeakPower>())
            await PowerCmd.Remove<WeakPower>(Owner.Creature);
        if (Owner.Creature.HasPower<VulnerablePower>())
            await PowerCmd.Remove<VulnerablePower>(Owner.Creature);
        if (Owner.Creature.HasPower<FrailPower>())
            await PowerCmd.Remove<FrailPower>(Owner.Creature);
    }
}
