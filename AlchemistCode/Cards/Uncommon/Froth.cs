using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Froth : AlchemistCard
{
    protected override int FermentPeak => 2;

    public Froth() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithKeyword(CardKeyword.Retain);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HitsLine", HitsLine(FermentTurns > 0 ? 1 + FermentTurns : 0));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hitCount = 1 + FermentTurns;
        await CommonActions.CardAttack(this, play, hitCount, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
    }
}
