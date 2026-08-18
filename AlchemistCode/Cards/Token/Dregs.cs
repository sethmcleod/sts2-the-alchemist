using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

// The Ferment byproduct. Base Toxic deals a flat 5, which Antitoxin cannot touch because it is
// card-sourced. Dosing instead routes the cost through the character's own resource, so holding one
// is free if the bar can cover it
[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Poison, CardTheme.Ferment)]
public class Dregs : AlchemistCard
{
    private const int SelfPoison = 2;

    public override int MaxUpgradeLevel => 0;

    public override bool HasTurnEndInHandEffect => true;

    protected internal override bool PlaysCastAnimation => false;

    public Dregs() : base(1, CardType.Status, CardRarity.Status, TargetType.None)
    {
        WithVar("Poison", SelfPoison, 0);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, DynamicVars["Poison"].IntValue, Owner.Creature, this);
    }
}
