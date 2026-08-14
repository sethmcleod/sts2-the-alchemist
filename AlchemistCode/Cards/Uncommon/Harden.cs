using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Harden : AlchemistCard
{
    public Harden() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Block", 2, 1);
        WithVar("Antitoxin", 4, 1);
        WithTip(StaticHoverTip.Block);
        WithTip(typeof(PoisonPower));
        WithTip(typeof(AntitoxinPower));
    }

    internal override bool GainsEffectWhenEnchanted => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<HardenPower>(choiceContext, Owner.Creature,
            DynamicVars["Block"].IntValue, Owner.Creature, this);
        if (IsEnchanted)
            await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, DynamicVars["Antitoxin"].IntValue, Owner.Creature, this);
    }
}
