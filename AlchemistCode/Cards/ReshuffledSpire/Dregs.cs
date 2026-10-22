using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[Pool(typeof(StatusCardPool))]
[CardTheme(CardTheme.Poison)]
public class Dregs : AlchemistReshuffledCard
{
    public override int MaxUpgradeLevel => 0;

    public Dregs() : base(-1, CardType.Status, CardRarity.Status, TargetType.None)
    {
        WithVar("SelfPoison", 2);
        WithCards(1);
        WithKeyword(CardKeyword.Unplayable);
        WithKeyword(CardKeyword.Ethereal);
        WithTip(typeof(PoisonPower));
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
        await CommonActions.Draw(this, choiceContext);
    }
}
