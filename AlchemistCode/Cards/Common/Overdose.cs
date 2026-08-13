using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Overdose : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Exhaust;

    public Overdose() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(15, 5);
        WithVar("hpLoss", 4, 0);
        WithVar("ReactionDraw", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var reacted = ReactionActive;
        await LoseHp(choiceContext, DynamicVars["hpLoss"].IntValue);
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_blunt")).Execute(choiceContext);
        if (reacted)
            await CardPileCmd.Draw(choiceContext, DynamicVars["ReactionDraw"].IntValue, Owner);
    }
}
