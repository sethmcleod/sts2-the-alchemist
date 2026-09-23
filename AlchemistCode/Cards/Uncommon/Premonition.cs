using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Premonition : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Premonition() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<PoisonPower>(3, 0);
        WithVar(new ScryVar(4));
        WithCards(1, 0);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Scrying.Execute(choiceContext, this);
        await CommonActions.Draw(this, choiceContext);
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
    }
}
