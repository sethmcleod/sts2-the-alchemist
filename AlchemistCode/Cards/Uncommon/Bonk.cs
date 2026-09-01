using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class Bonk : AlchemistCard
{
    public Bonk() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(10, static (card, _) =>
                (card.IsUpgraded ? 4m : 3m) * Mixing.PlayedThisCombat(card.Owner),
            ValueProp.Move, 2, 0);
        WithTips(_ => Mixing.MixTips());
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // Live count only in combat; the compendium and reward previews show the bare sentence
        description.Add("MixesPlayed",
            IsMutable && CombatState != null ? $" ({Mixing.PlayedThisCombat(Owner)})" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab"),
            tmpSfx: "heavy_attack.mp3").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
    }
}
