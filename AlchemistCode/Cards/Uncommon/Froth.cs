using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class Froth : AlchemistCard
{
    protected override bool Ferments => true;

    public Froth() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithKeyword(CardKeyword.Retain);
    }

    private const int MaxHits = 4;

    private int Hits => Math.Min(1 + FermentTurns, MaxHits);

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HitsLine", HitsLine(FermentTurns > 0 ? Hits : 0));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
    }
}
