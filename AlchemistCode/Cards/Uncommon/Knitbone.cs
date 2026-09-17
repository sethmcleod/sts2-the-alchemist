using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class Knitbone : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Knitbone() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(5, 2, static (card, _) => Mixing.PlayedThisCombat(card.Owner), ValueProp.Move, 2);
        WithTips(_ => Mixing.MixTips());
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("MixesPlayed",
            IsMutable && CombatState != null ? $" ({Mixing.PlayedThisCombat(Owner)})" : "");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
