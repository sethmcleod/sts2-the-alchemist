using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class UnstableCompound : AlchemistCard
{
    protected override bool IsSeepCard => true;

    public UnstableCompound() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(16, 6);
        WithTips(_ => new[] { HoverTipFactory.FromCard<Toxic>() });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }

    // The mixture degrades in your hand: each held turn adds a base-game Toxic status
    protected override async Task OnSeep(PlayerChoiceContext choiceContext)
    {
        if (CombatState is not { } combat) return;
        var toxic = combat.CreateCard<Toxic>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(toxic, PileType.Hand, Owner);
    }
}
