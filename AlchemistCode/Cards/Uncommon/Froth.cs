using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class Froth : AlchemistCard
{
    protected override bool Ferments => true;

    public Froth() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(5, 2);
        WithVar(new FermentVar("Hits", 1, 1));
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
    }

    private int Hits => ((FermentVar)DynamicVars["Hits"]).Total(this, null);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_slime_impact"))
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay).Execute(choiceContext);
    }
}
