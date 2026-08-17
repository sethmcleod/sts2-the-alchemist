using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Basic;

// Block sits between Defend's 5 and Congeal's 7, because the rarity ladder wants a Basic below the
// Common of the same shape. Survivor is the only other non-Defend Basic block card in the game at
// 8 (11), and it pays for that with a discard
public class Antidote : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Antidote() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(6, 3);
        WithVar("antitoxin", 2, 1);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
