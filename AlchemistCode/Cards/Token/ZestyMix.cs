using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Mix, CardTheme.Antitoxin)]
public class ZestyMix : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public ZestyMix() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithCards(1, 0);
        WithVar("antitoxin", 3, 2);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
    }
}
