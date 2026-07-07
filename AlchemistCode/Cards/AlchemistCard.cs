using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards;

[Pool(typeof(AlchemistCardPool))]
public abstract class AlchemistCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Internal so the static calc-damage lambdas can read it off the card arg (they must capture no instance state)
    internal bool IsReduced => Owner?.Creature is { } c && c.CurrentHp * 2 <= c.MaxHp;

    protected virtual bool IsGambitCard => false;

    protected override bool ShouldGlowGoldInternal => IsGambitCard && IsReduced;

    // Formula-damage cards deal raw DamageCmd.Attack(decimal) with no DamageVar, so the base game's enchant
    // preview never applies or shows the bonus. They fold EnchantDamageBonus in and render {EnchantBonus}
    protected virtual bool HasFormulaDamage => false;

    internal int EnchantDamageBonus =>
        Enchantment == null ? 0 : (int)Enchantment.EnchantDamageAdditive(0m, ValueProp.Move);

    private int _fermentTurns;

    protected virtual bool IsFermentCard => false;

    internal bool IsFermentInline => IsFermentCard;

    // Internal so the static calc lambdas can read it off the card arg (no instance capture)
    internal int FermentTurns => _fermentTurns;

    protected virtual string FermentTotalText => "";

    protected virtual bool IsSeepCard => false;

    protected virtual Task OnSeep(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // Ferment and Seep only fire while the card is held in hand at the owner's turn end
        if (Owner == null || !participants.Contains(Owner.Creature)
            || !PileType.Hand.GetPile(Owner).Cards.Contains(this))
            return;
        if (IsFermentCard) _fermentTurns++;
        if (IsSeepCard)
        {
            CardCmd.Preview(new[] { this });
            await OnSeep(choiceContext);
        }
    }

    // Returns the fermented-turn count and resets it
    protected int ConsumeFermentTurns()
    {
        var turns = _fermentTurns;
        _fermentTurns = 0;
        return turns;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        if (IsFermentCard)
        {
            description.Add("FermentSuffix", _fermentTurns > 0 ? $" ({_fermentTurns})" : "");
            description.Add("FermentTotal", FermentTotalText);
        }
        if (HasFormulaDamage)
        {
            var bonus = EnchantDamageBonus;
            description.Add("EnchantBonus", bonus > 0 ? $" [green]+ {bonus}[/green]" : "");
        }
    }
}
