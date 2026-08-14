using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Siphon : AlchemistCard
{
    public Siphon() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithCards(1, 0);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        // Infusing what it just drew keeps this distinct from Next Up, which infuses a chosen card,
        // and it needs no selection screen, which suits a Common
        foreach (var drawn in await CommonActions.Draw(this, choiceContext))
            Infusion.Infuse(drawn);
    }
}
