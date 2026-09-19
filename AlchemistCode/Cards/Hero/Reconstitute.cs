using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Hero;

// The Hero Expansion's Mysterious Flashlight hands this over as the Alchemist's card from the past
[Pool(typeof(EventCardPool))]
[CardTheme(CardTheme.Poison)]
public class Reconstitute : AlchemistHeroCard
{
    // The Event pool files it under the compendium's Other section; the frame stays the Alchemist's
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<Character.AlchemistCardPool>();

    private static LocString Prompt => new("cards", "ALCHEMIST-RECONSTITUTE.selectionScreenPrompt");

    public Reconstitute() : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
        WithVar("SelfPoison", 3, 0);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
        WithTip(typeof(PoisonPower));
        HeroExpansion.RegisterFlashlight(this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
        var discard = PileType.Discard.GetPile(Owner);
        if (discard.Cards.Count == 0) return;
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, discard, Owner,
            new CardSelectorPrefs(Prompt, 1), static _ => true)).FirstOrDefault();
        if (chosen != null) await CardPileCmd.Add(chosen, PileType.Hand);
    }
}
