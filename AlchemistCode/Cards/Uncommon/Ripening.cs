using System.Linq;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment)]
public class Ripening : AlchemistCard
{
    public Ripening() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<RipeningPower>(1, 0);
        WithTips(card => card.IsUpgraded
            ? new[] { AlchemistTips.FermentRef }.Concat(Mixing.MixTips())
            : new[] { AlchemistTips.FermentRef });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<RipeningPower>(choiceContext, this);
        if (IsUpgraded && Owner.Creature.GetPower<RipeningPower>() is { } ripening)
            ripening.AddMixes(1);
    }
}
