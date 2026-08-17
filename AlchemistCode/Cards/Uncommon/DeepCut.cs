using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

// The tempo attack. It used to contain Puncture, which kept the Vulnerable and now owns it
[CardTheme(CardTheme.None)]
public class DeepCut : AlchemistCard
{
    public DeepCut() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(13, 3);
        WithVar("Cards", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, Owner);
    }
}
