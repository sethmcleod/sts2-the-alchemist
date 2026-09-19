using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

// What the Ancient Scepter turns every Strike into. The dose lands before the hit, so the hit reads
// it; the face adds it to the preview until the play is resolving, when the real stack already has it
[CardTheme(CardTheme.Poison, CardTheme.Antitoxin)]
public class PotentStrike : AlchemistAncientsCard
{
    public override CardPoolModel VisualCardPool =>
        AncientsAwakenedMod.PerfectedPoolOr(ModelDb.CardPool<Character.AlchemistCardPool>());

    public PotentStrike() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) =>
            Dose(card) + (card is PotentStrike { _resolving: false } p ? p.DynamicVars["SelfPoison"].IntValue : 0),
            ValueProp.Move, 3);
        WithKeyword(AlchemistKeywords.Laced);
        WithVar("SelfPoison", 1, 0);
        WithVar("Antitoxin", 1, 0);
        WithTags(CardTag.Strike);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    private bool _resolving;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        _resolving = true;
        try
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
                DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
            await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
                DynamicVars["Antitoxin"].IntValue, Owner.Creature, this);
            await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        }
        finally
        {
            _resolving = false;
        }
    }
}
