using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Fumigate : AlchemistCard
{
    public Fumigate() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(2, 0);
        WithKeyword(CardKeyword.Exhaust);
    }

    // A null CombatState means the deck view or the compendium, where the count is 0
    private int ExhaustCount =>
        IsMutable && CombatState != null ? PileType.Exhaust.GetPile(Owner).Cards.Count : 0;

    protected override bool ConditionalGlow => ExhaustCount > 0;

    // The shared live hit-count line, at the bottom of the card like Overflow and Fighting Spirits
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HitsLine",
            HitsLine(ExhaustCount is var n and > 0 ? 1 + n : 0));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hitCount = 1 + ExhaustCount;
        await CommonActions.CardAttack(this, play, hitCount, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
    }
}
