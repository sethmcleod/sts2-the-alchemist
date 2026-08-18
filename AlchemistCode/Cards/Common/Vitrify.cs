using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

// The plain sponge. One number, no rider: it is the depth of the dose you can carry this fight
[CardTheme(CardTheme.Antitoxin)]
public class Vitrify : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Vitrify() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("antitoxin", 6, 3);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
