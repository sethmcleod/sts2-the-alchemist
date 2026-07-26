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

    // A null CombatState is the deck view or the compendium, where the count is 0
    private int PotionsUsedThisCombat =>
        CombatState == null ? 0 : CombatManager.Instance.History.Entries.OfType<PotionUsedEntry>().Count();

    protected override bool ConditionalGlow => PotionsUsedThisCombat > 0;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HitsLine",
            HitsLine(PotionsUsedThisCombat is var n and > 0 ? 1 + n : 0));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var hitCount = 1 + PotionsUsedThisCombat;
        await CommonActions.CardAttack(this, play, hitCount, vfx: HitVfx("vfx/vfx_attack_blunt"), tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
    }
}
