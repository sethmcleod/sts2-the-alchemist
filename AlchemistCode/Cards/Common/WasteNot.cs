using System.Linq;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Ferment, CardTheme.Antitoxin)]
public class WasteNot : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public WasteNot() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(4, 3);
        WithVar("antitoxin", 2, 0);
        WithTip(typeof(AntitoxinPower));
        WithUpgradingCardTip<Dregs>();
    }

    private IEnumerable<Dregs> HeldDregs =>
        !IsMutable || Owner == null
            ? Enumerable.Empty<Dregs>()
            : PileType.Hand.GetPile(Owner).Cards.OfType<Dregs>();

    protected override bool ConditionalGlow => HeldDregs.Any();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);

        var dregs = HeldDregs.ToList();
        if (dregs.Count == 0) return;
        foreach (var card in dregs)
            await CardCmd.Exhaust(choiceContext, card);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue * dregs.Count, Owner.Creature, this);
    }
}
