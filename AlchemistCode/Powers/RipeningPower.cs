using MegaCrit.Sts2.Core.Entities.Powers;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class RipeningPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // The upgrade's Mix rides in a DynamicVar so the smart hover text can print it (the smart path
    // only adds Amount and the DynamicVars); the card sets it after applying the power
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Mixes", 0) };

    internal void AddMixes(int mixes) => DynamicVars["Mixes"].BaseValue += mixes;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => DynamicVars["Mixes"].IntValue > 0
        ? new[] { AlchemistTips.FermentRef }.Concat(Mixing.MixTips())
        : new[] { AlchemistTips.FermentRef };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (!PileType.Hand.GetPile(player).Cards.OfType<AlchemistCard>().Any(c => c.IsFermentInline)) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, (int)Amount, player);
        for (var i = 0; i < DynamicVars["Mixes"].IntValue; i++)
            await Mixing.CreateRandom(choiceContext, player, source: this);
    }
}
