using Alchemist.AlchemistCode.Cards.Ancient;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Basic;

[CardTheme(CardTheme.Poison)]
public class Jab : AlchemistCard, ITranscendenceCard
{
    public Jab() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) => Dose(card), ValueProp.Move, 3);
        WithKeyword(AlchemistKeywords.Laced);
        WithVar("Poison", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Wormwood>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        if (play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target,
            DynamicVars["Poison"].IntValue, Owner.Creature, this);
    }
}
