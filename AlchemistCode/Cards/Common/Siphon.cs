using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Infuse)]
public class Siphon : AlchemistCard
{
    public Siphon() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithCards(1, 0);
        WithVar("antitoxin", 1, 0);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        foreach (var drawn in await CommonActions.Draw(this, choiceContext))
        {
            if (Infusion.CanInfuse(drawn))
                Infusion.Infuse(drawn);
            else
                await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
                    DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
        }
    }
}
