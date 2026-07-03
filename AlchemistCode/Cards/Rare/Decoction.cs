using Alchemist.AlchemistCode;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Decoction : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public Decoction() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1); // 1 (0)
        WithTip(typeof(PoisonPower));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Seep) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null, this);
        foreach (var card in selected)
            await CardCmd.Exhaust(choiceContext, card);

        // Procure a random combat-eligible potion of any rarity.
        var rng = Owner.RunState.Rng.CombatPotionGeneration;
        var options = PotionFactory.GetPotionOptions(Owner, System.Array.Empty<PotionModel>())
            .Where(p => p.CanBeGeneratedInCombat)
            .ToList();
        var potion = rng.NextItem(options);
        if (potion != null)
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
    }

    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }
}
