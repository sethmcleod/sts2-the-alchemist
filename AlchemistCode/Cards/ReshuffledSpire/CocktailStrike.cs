using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Mix)]
public class CocktailStrike : AlchemistReshuffledCard
{
    public CocktailStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(4, 4, static (card, _) => KindsPlayed(card), ValueProp.Move, 0, 1);
        WithTags(CardTag.Strike);
        WithTips(_ => Mixing.MixRefTips());
    }

    private static decimal KindsPlayed(CardModel card) =>
        card is AlchemistCard { IsMutable: true, Owner: { } owner } ? Mixing.KindsPlayedThisCombat(owner) : 0m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
    }
}
