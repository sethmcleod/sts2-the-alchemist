using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Antitoxin)]
public class Quench : AlchemistCard
{
    // Block per point of capacity
    private const int BlockPerPoint = 2;

    protected internal override bool PlaysCastAnimation => false;

    public Quench() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("antitoxin", 4, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithCalculatedBlock(0, static (card, _) => BlockFrom(card), ValueProp.Move);
        WithTip(typeof(AntitoxinPower));
    }

    // The pending gain is part of the total the text promises, so the Block lands first and the
    // Antitoxin second; granting first would make the calc count the gain twice
    private static decimal BlockFrom(CardModel card) =>
        card is Quench { IsMutable: true, Owner.Creature: { } creature } quench
            ? (creature.GetPowerAmount<AntitoxinPower>()
               + quench.DynamicVars["antitoxin"].IntValue) * BlockPerPoint
            : 0m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
