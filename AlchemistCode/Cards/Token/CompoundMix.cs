using System.Collections.Generic;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Token;

// One token for every pairing. The two ingredient kinds are set by Compose right after creation, and
// the type, target, title and text all derive from them, so a Bursting and a Syrupy read as an
// Attack aimed at an enemy while a Zesty and a Syrupy read as a self Skill
[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Mix)]
public class CompoundMix : AlchemistCard
{
    private MixKind _first = MixKind.Bursting;
    private MixKind _second = MixKind.Syrupy;
    // The compendium and the click view build mutable copies of the canonical card, so mutability is
    // not the test for a made one; only Compose sets this
    private bool _composed;

    // Hidden from the card library the way Mad Science is: an unmade one has nothing to show
    public CompoundMix() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false)
    {
        WithDamage(6);
        WithBlock(4);
        WithCards(1);
        WithVar("Bonus", 1);
        // Plain vars rather than PowerVars, so no tip attaches by itself; IngredientTips adds the
        // ones the two ingredients actually call for
        WithVar("Weak", 1);
        WithVar("Vulnerable", 1);
        WithVar("Poison", 3);
        WithVar("SelfPoison", 1);
        WithVar("Energy", 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(card => ((CompoundMix)card).IngredientTips());
    }

    private IEnumerable<IHoverTip> IngredientTips()
    {
        if (Has(MixKind.Syrupy)) yield return HoverTipFactory.Static(StaticHoverTip.Block);
        if (Has(MixKind.Fuming))
        {
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
        if (Has(MixKind.Fuming) || Has(MixKind.Acrid)) yield return HoverTipFactory.FromPower<PoisonPower>();
        if (Has(MixKind.Sparkling)) yield return HoverTipFactory.Static(StaticHoverTip.Energy);
    }

    private bool Has(MixKind kind) => _first == kind || _second == kind;

    public override CardType Type => Has(MixKind.Bursting) ? CardType.Attack : CardType.Skill;

    public override TargetType TargetType =>
        Has(MixKind.Bursting) || Has(MixKind.Fuming) || Has(MixKind.Acrid) ? TargetType.AnyEnemy : TargetType.Self;

    public override bool GainsBlock => Has(MixKind.Syrupy);

    protected internal override bool PlaysCastAnimation => !Has(MixKind.Bursting);

    // The ingredients' own numbers are copied rather than upgrade deltas re-declared here, so a
    // Bursting+ in the pair gives the compound the 9 and the token class stays the single source
    internal void Compose(CardModel first, CardModel second)
    {
        _first = Mixing.KindOf(first) ?? MixKind.Bursting;
        _second = Mixing.KindOf(second) ?? MixKind.Syrupy;
        _composed = true;
        Absorb(first, add: false);
        // A pair of the same kind is one effect at double strength, not the same line twice
        Absorb(second, add: _first == _second);
    }

    private bool Doubled => _first == _second;

    private void Absorb(CardModel mix, bool add)
    {
        void Set(string name, decimal value) =>
            DynamicVars[name].BaseValue = (add ? DynamicVars[name].BaseValue : 0m) + value;
        switch (mix)
        {
            case BurstingMix:
                Set("Damage", mix.DynamicVars.Damage.BaseValue);
                break;
            case SyrupyMix:
                Set("Block", mix.DynamicVars.Block.BaseValue);
                break;
            case ZestyMix:
                Set("Cards", mix.DynamicVars.Cards.BaseValue);
                break;
            case FumingMix:
                Set("Weak", mix.DynamicVars.Weak.BaseValue);
                Set("Vulnerable", mix.DynamicVars.Vulnerable.BaseValue);
                Set("SelfPoison", mix.DynamicVars["SelfPoison"].BaseValue);
                break;
            case AcridMix:
                Set("Poison", mix.DynamicVars.Poison.BaseValue);
                break;
            case SparklingMix:
                Set("Energy", mix.DynamicVars.Energy.BaseValue);
                break;
        }
    }

    private static string Key(MixKind kind) => kind.ToString().ToLowerInvariant();

    // An unmade copy keeps the plain name; a made one names its ingredients
    public override string Title
    {
        get
        {
            if (!_composed) return base.Title;
            var title = new LocString("cards", _first == _second
                ? "ALCHEMIST-COMPOUND_MIX.titleFormatSame"
                : "ALCHEMIST-COMPOUND_MIX.titleFormat");
            title.Add("First", new LocString("cards", $"ALCHEMIST-COMPOUND_MIX.name_{Key(_first)}").GetFormattedText());
            title.Add("Second", new LocString("cards", $"ALCHEMIST-COMPOUND_MIX.name_{Key(_second)}").GetFormattedText());
            return title.GetFormattedText();
        }
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Body", !_composed ? "??????" : Doubled ? Part(_first) : Part(_first) + "\n" + Part(_second));
    }

    private string Part(MixKind kind)
    {
        var part = new LocString("cards", $"ALCHEMIST-COMPOUND_MIX.part_{Key(kind)}");
        switch (kind)
        {
            case MixKind.Bursting: part.Add(DynamicVars.Damage); break;
            case MixKind.Syrupy: part.Add(DynamicVars.Block); break;
            case MixKind.Zesty: part.Add(DynamicVars.Cards); break;
            case MixKind.Fuming:
                part.Add(DynamicVars["Weak"]);
                part.Add(DynamicVars["Vulnerable"]);
                part.Add(DynamicVars["SelfPoison"]);
                break;
            case MixKind.Acrid: part.Add(DynamicVars["Poison"]); break;
            // The card-side energyIcons formatter wants an EnergyVar; the potion-side form takes the
            // prefix and a literal count, and a Sparkling is always 1
            case MixKind.Sparkling:
                if (DynamicVars["Energy"].IntValue > 1)
                    part = new LocString("cards", "ALCHEMIST-COMPOUND_MIX.part_sparkling_double");
                part.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
                break;
        }
        return part.GetFormattedText();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars["Bonus"].IntValue, Owner);
        await Resolve(_first, choiceContext, play);
        if (!Doubled) await Resolve(_second, choiceContext, play);
    }

    private async Task Resolve(MixKind kind, PlayerChoiceContext choiceContext, CardPlay play)
    {
        switch (kind)
        {
            case MixKind.Bursting:
                await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_fire_burst"),
                    sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
                break;
            case MixKind.Syrupy:
                await CommonActions.CardBlock(this, play);
                break;
            case MixKind.Zesty:
                await CommonActions.Draw(this, choiceContext);
                break;
            case MixKind.Fuming:
                if (play.Target is not { IsAlive: true } debuffed) return;
                await PowerCmd.Apply<WeakPower>(choiceContext, debuffed, DynamicVars["Weak"].IntValue, Owner.Creature, this);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, debuffed, DynamicVars["Vulnerable"].IntValue, Owner.Creature, this);
                await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
                break;
            case MixKind.Acrid:
                if (play.Target is not { IsAlive: true } poisoned) return;
                PoisonSplash(poisoned);
                await PowerCmd.Apply<PoisonPower>(choiceContext, poisoned, DynamicVars["Poison"].IntValue, Owner.Creature, this);
                break;
            case MixKind.Sparkling:
                await PlayerCmd.GainEnergy(DynamicVars["Energy"].BaseValue, Owner);
                break;
        }
    }
}
