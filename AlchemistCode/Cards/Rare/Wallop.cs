using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Decant)]
public class Wallop : AlchemistCard
{
    public Wallop() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        // A full level triples the hit, so the extra term is twice the live base
        WithCalculatedDamage(12, static (card, _) =>
                card is Wallop { DecantFull: true } ? 2m * (card.IsUpgraded ? 16m : 12m) : 0m,
            ValueProp.Move, 4, 0);
        WithVar("DecantMax", 4, -1);
    }

    protected override bool Decants => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Spend AFTER the attack: the calculated damage reads the live level while the hit resolves
        var primed = DecantFull;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_heavy_blunt"),
                tmpSfx: "heavy_attack.mp3").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
        if (primed) TrySpendDecant();
    }
}
