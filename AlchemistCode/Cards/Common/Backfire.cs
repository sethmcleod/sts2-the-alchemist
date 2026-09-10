using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.None)]
public class Backfire : AlchemistCard
{
    public Backfire() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithVar(new IntentHitsVar("Hits", 1));
    }

    private int Hits(Creature? target) => ((IntentHitsVar)DynamicVars["Hits"]).Total(this, target);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits(play.Target), vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
    }
}
