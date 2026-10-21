using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Poison, CardTheme.Antitoxin, CardTheme.Mix)]
public class HeroicDose : AlchemistReshuffledCard
{
    protected internal override bool PlaysCastAnimation => false;

    public HeroicDose() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
        WithVar("antitoxin", 3, 1);
        WithVar("SelfPoison", 3, 1);
        WithUpgradingCardTip<Token.BurstingMix>();
        WithUpgradingCardTip<Token.SyrupyMix>();
        WithUpgradingCardTip<Token.ZestyMix>();
        WithTip(typeof(AntitoxinPower));
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var mix = await Mixing.Choose(choiceContext, Owner, upgraded: IsUpgraded, source: this);
        if (mix != null) await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
            DynamicVars["antitoxin"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
    }
}
