using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Ancient;

// The Ancient's gift is the loop on one Power: a dose every turn and the cover for it. It used to
// procure a potion every turn, which rewarded stalling and was Meta Scaling by the rubric's own name
[CardTheme(CardTheme.Poison, CardTheme.Antitoxin)]
public class Elixir : AlchemistCard
{
    public Elixir() : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
        WithVar("SelfPoison", 2, 0);
        WithVar("antitoxin", 3, 1);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ElixirPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
