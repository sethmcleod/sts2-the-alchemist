using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class RestockPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => Mixing.MixRefTips();

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player is not { } player) return;
        for (var i = 0; i < Amount; i++)
        {
            var mixes = PileType.Exhaust.GetPile(player).Cards.Where(IsBasicMix).ToList();
            if (mixes.Count == 0) return;
            if (i == 0) Flash();
            await CardPileCmd.Add(player.RunState.Rng.CombatCardSelection.NextItem(mixes)!, PileType.Draw,
                CardPilePosition.Random);
        }
    }

    private static bool IsBasicMix(CardModel card) => Mixing.KindOf(card) is { } kind && Mixing.Basic.Contains(kind);
}
