using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Cards.Token;

namespace TheAlchemist.TheAlchemistCode.Cards.Basic;

public class Nigredo : TheAlchemistCard
{
    public Nigredo() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithPower<PoisonPower>(2, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Albedo>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;

        var allCreatures = CombatState.Enemies
            .Concat(CombatState.PlayerCreatures)
            .Where(c => c.IsAlive);
        foreach (var creature in allCreatures)
            await PowerCmd.Apply<PoisonPower>(choiceContext, creature, DynamicVars.Poison.BaseValue, Owner.Creature, this);

        CardModel albedo = CombatState.CreateCard<Albedo>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(albedo);
        await CardPileCmd.AddGeneratedCardToCombat(albedo, PileType.Hand, Owner);
    }
}
