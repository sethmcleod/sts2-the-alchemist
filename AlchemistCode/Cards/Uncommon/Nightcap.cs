using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Antitoxin)]
public class Nightcap : AlchemistCard
{
    public Nightcap() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVar("antitoxin", 2, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(AntitoxinPower));
    }

    private int Grant => DynamicVars["antitoxin"].IntValue;

    private int Mult => IsUpgraded ? 3 : 2;

    protected override int? RawFormulaDamagePreview =>
        IsMutable && CombatState != null ? (AntitoxinCapacity + Grant) * Mult : null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null || play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, Grant, Owner.Creature, this);
        var damage = AntitoxinCapacity * Mult;
        if (damage <= 0) return;
        await CommonActions.CardAttack(this, play, target, damage, ValueProp.Move,
                vfx: HitVfx("vfx/vfx_attack_blunt"), tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}
