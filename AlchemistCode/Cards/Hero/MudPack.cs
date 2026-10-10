using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Hero;

[CardTheme(CardTheme.Poison)]
public class MudPack : AlchemistHeroCard
{
    protected override bool HasEnergyCostX => true;
    protected internal override bool PlaysCastAnimation => false;

    public MudPack() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithBlock(7, 0);
        WithVar("Extra", 0, 1);
        WithCalculatedVar("TotalBlock", 0, static (card, _) => card.DynamicVars.Block.PreviewValue * PreviewTimes(card));
        WithTip(BaseLibTip.Scry);
        WithTip(typeof(PoisonPower));
    }

    private static int PreviewTimes(CardModel card)
    {
        if (card is not MudPack { IsMutable: true, CombatState: { } combat } self) return 0;
        var x = self.Pile?.Type == PileType.Play ? self.EnergyCost.CapturedXValue : self.EnergyCost.GetAmountToSpend();
        return Math.Max(0, Hook.ModifyXValue(combat, self, x) + self.DynamicVars["Extra"].IntValue);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var times = ResolveEnergyXValue() + DynamicVars["Extra"].IntValue;
        if (times <= 0) return;
        await Scrying.Execute(choiceContext, Owner, times);
        for (var i = 0; i < times; i++)
            await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, times, Owner.Creature, this);
    }
}
