using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Fumigate : AlchemistCard
{
    public Fumigate() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(1, 1);
        WithKeyword(CardKeyword.Exhaust);
    }

    // A null CombatState means the deck view or the compendium, where the count is 0
    private int ExhaustCount =>
        IsMutable && CombatState != null ? PileType.Exhaust.GetPile(Owner).Cards.Count : 0;

    // The card exhausts itself, but that lands after the attack, so it never counts toward its own hits
    private int HitCount => 1 + ExhaustCount / 2;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // Only once the pile actually buys a hit. Below 2 cards the line would just restate the single
        // hit the card already describes
        description.Add("HitsLine", HitsLine(ExhaustCount >= 2 ? HitCount : 0));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, HitCount, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
    }
}
