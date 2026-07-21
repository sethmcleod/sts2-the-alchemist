using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class FightingSpirits : AlchemistCard
{
    public FightingSpirits() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(10, 4);
    }

    // The extra hits equal the potions used this combat. A null CombatState means the card is not in a live
    // combat, for example the deck view or the compendium, where the count is 0 and the card does not glow
    private int PotionsUsedThisCombat =>
        CombatState == null ? 0 : CombatManager.Instance.History.Entries.OfType<PotionUsedEntry>().Count();

    // Glow gold once a potion has been used, because the card then hits more than once. IsMutable already
    // gates ConditionalGlow, so this is safe on the canonical model
    protected override bool ConditionalGlow => PotionsUsedThisCombat > 0;

    // Show the potions used so far in green, the same live parentheses the damage previews use
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("PotionsUsed",
            IsMutable && PotionsUsedThisCombat is var n and > 0 ? $" ([green]{n}[/green])" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var hitCount = 1 + PotionsUsedThisCombat;
        await CommonActions.CardAttack(this, play, hitCount).Execute(choiceContext);
    }
}
