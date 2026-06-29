using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheAlchemist.TheAlchemistCode.Character;
using TheAlchemist.TheAlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace TheAlchemist.TheAlchemistCode.Cards;

[Pool(typeof(TheAlchemistCardPool))]
public abstract class TheAlchemistCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}
