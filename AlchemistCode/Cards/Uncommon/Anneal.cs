using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Anneal : AlchemistCard
{
    public Anneal() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 0);
        WithVar("Regen", 2, 1);
        WithTip(typeof(RegenPower));
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override bool ConditionalGlow => IsEnchanted;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
        if (IsEnchanted)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature,
                DynamicVars["Regen"].IntValue, Owner.Creature, this);
    }
}
