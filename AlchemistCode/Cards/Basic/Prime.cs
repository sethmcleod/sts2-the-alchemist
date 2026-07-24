using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Prime : AlchemistCard, ITranscendenceCard
{
    protected override bool IsGambitCard => true;

    public Prime() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithBlock(4, 2);
        WithTips(_ => Infusion.InfuseTips());
    }

    // Prime is the Alchemist's transcendence starter. BaseLib reads this to build Archaic Tooth's upgrade
    // map, which upgrades Prime into Aureate. Dusty Tome reads the same map to keep Aureate out of its
    // Ancient pool, so Aureate can come only from Prime. This replaces a hand-rolled ArchaicTooth patch
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Aureate>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
        if (IsReduced)
            await CommonActions.CardBlock(this, play);
    }
}
