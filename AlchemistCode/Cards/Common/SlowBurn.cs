using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Ferment)]
public class SlowBurn : AlchemistCard
{
    protected override bool Ferments => true;

    public SlowBurn() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithKeyword(CardKeyword.Retain);
    }

    internal int Burn => FermentTurns;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("BurnNow", FermentTurns > 0 ? $" ([green]{Burn}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
            sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
    }
}
