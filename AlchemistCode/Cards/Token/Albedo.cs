using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Albedo : AlchemistCard
{
    public Albedo() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithPower<RegenPower>(0, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Citrinitas>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;

        var poisonAmount = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poisonAmount > 0)
        {
            await PowerCmd.Remove<PoisonPower>(Owner.Creature);
            var regenAmount = poisonAmount + (IsUpgraded ? 1 : 0);
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, regenAmount, Owner.Creature, this);
        }

        await AlchemistCardCmd.GiveCard<Citrinitas>(this);
    }
}
