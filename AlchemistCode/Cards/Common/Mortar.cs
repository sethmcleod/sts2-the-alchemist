using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Ferment)]
public class Mortar : AlchemistCard
{
    protected override bool Ferments => true;

    public override bool GainsBlock => true;

    public Mortar() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(9, static (card, _) =>
                3m * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 3, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var attack = await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact"))
            .Execute(choiceContext);
        // Fisticuffs' rule: the block is what the hit actually dealt, overkill included
        await CreatureCmd.GainBlock(Owner.Creature,
            attack.Results.SelectMany(r => r).Sum(r => r.TotalDamage + r.OverkillDamage),
            ValueProp.Move, play);
    }
}
