using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Effluvium : AlchemistCard
{
    public Effluvium() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(1, 1);       // applied to the target: 1 (2)
        WithPower<VulnerablePower>(1, 1); // applied to the target: 1 (2)
        WithVar("SelfPoison", 1, 1);      // gained by you: 1 (2)
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<WeakPower>(choiceContext, this, play);
        await CommonActions.Apply<VulnerablePower>(choiceContext, this, play);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].BaseValue, Owner.Creature, this);
    }
}
