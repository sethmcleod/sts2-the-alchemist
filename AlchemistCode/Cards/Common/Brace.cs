using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.None)]
public class Brace : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Brace() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(5, static (card, _) => Threatened(card) ? Bonus(card) : 0m, ValueProp.Move, 2);
        WithVar("Bonus", 4, 1);
    }

    private static bool Threatened(CardModel card) =>
        card is Brace { IsMutable: true, CombatState: { } combat }
        && combat.Enemies.Any(e => e.IsAlive && e.Monster is { IntendsToAttack: true });

    private static decimal Bonus(CardModel card) => card.DynamicVars["Bonus"].IntValue;

    protected override bool ConditionalGlow => Threatened(this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
