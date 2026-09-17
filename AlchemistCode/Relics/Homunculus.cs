using System.Linq;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Relics;

public class Homunculus : AlchemistRelic
{
    private const int Turns = 1;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { AlchemistTips.FermentRef };

    private static LocString Prompt => new("relics", "ALCHEMIST-HOMUNCULUS.selectionScreenPrompt");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;
        var draw = PileType.Draw.GetPile(Owner);
        if (!draw.Cards.Any(AlchemistCard.IsBrewing)) return;
        Flash();
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, draw, Owner,
            new CardSelectorPrefs(Prompt, 1), AlchemistCard.IsBrewing)).FirstOrDefault();
        if (chosen is not AlchemistCard ferment) return;
        await CardPileCmd.Add(ferment, PileType.Hand);
        await ferment.AdvanceFerment(Turns);
        CardCmd.Preview(ferment);
    }
}
