using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using System.Linq;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Afterglow : AlchemistCard
{
    public Afterglow() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
    }

    // Combat history is shared, so the Actor filter keeps this to the owner's own Potions rather than
    // counting an ally drinking in multiplayer
    private int PotionsUsedThisCombat =>
        !IsMutable || CombatState == null
            ? 0
            : CombatManager.Instance.History.Entries.OfType<PotionUsedEntry>()
                .Count(e => e.Actor == Owner.Creature);

    protected override bool ConditionalGlow => PotionsUsedThisCombat > 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("UsedLine",
            PotionsUsedThisCombat is var n and > 0
                ? PreviewLine("ALCHEMIST-POTIONS_USED_LINE", "Potions", n)
                : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var used = PotionsUsedThisCombat;
        if (used <= 0) return;
        await PlayerCmd.GainEnergy(used, Owner);
        await CardPileCmd.Draw(choiceContext, used, Owner);
    }
}
