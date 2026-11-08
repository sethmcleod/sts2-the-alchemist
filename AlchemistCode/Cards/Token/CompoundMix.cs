using System.Collections.Generic;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Compat;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
[CardTheme(CardTheme.Mix)]
public class CompoundMix : AlchemistCard
{
    private MixKind _first = MixKind.Bursting;
    private MixKind _second = MixKind.Syrupy;
    // Compendium copies are mutable too, so only Compose may set this
    private bool _composed;

    public CompoundMix() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false)
    {
        WithDamage(6);
        WithVar(new DamageVar("FumingDamage", 3, ValueProp.Move));
        WithBlock(4);
        WithCards(1);
        WithVar("Bonus", 1, 1);
        WithVar("Weak", 1);
        WithVar("Vulnerable", 1);
        WithVar("Tainted", 2);
        WithVar("SelfPoison", 1);
        WithVar("Energy", 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTips(card => ((CompoundMix)card).IngredientTips());
    }

    private IEnumerable<IHoverTip> IngredientTips()
    {
        if (Has(MixKind.Syrupy)) yield return HoverTipFactory.Static(StaticHoverTip.Block);
        if (Has(MixKind.Fuming)) yield return HoverTipFactory.FromPower<TaintedPower>();
        if (Has(MixKind.Acrid))
        {
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<PoisonPower>();
        }
        if (Has(MixKind.Sparkling)) yield return HoverTipFactory.Static(StaticHoverTip.Energy);
    }

    private bool Has(MixKind kind) => _first == kind || _second == kind;

    internal IEnumerable<MixKind> Ingredients => new[] { _first, _second };

    public override CardType Type =>
        Has(MixKind.Bursting) || Has(MixKind.Fuming) ? CardType.Attack : CardType.Skill;

    public override TargetType TargetType =>
        Has(MixKind.Bursting) || Has(MixKind.Fuming) || Has(MixKind.Acrid) ? TargetType.AnyEnemy : TargetType.Self;

    public override bool GainsBlock => Has(MixKind.Syrupy);

    protected internal override bool PlaysCastAnimation => Type != CardType.Attack;

    internal void Compose(CardModel first, CardModel second)
    {
        _first = Mixing.KindOf(first) ?? MixKind.Bursting;
        _second = Mixing.KindOf(second) ?? MixKind.Syrupy;
        _composed = true;
        Absorb(first, add: false);
        Absorb(second, add: _first == _second);
        if (Has(MixKind.Zesty)) DynamicVars["Bonus"].BaseValue += DynamicVars.Cards.BaseValue;
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
                Set("FumingDamage", mix.DynamicVars.Damage.BaseValue);
                Set("Tainted", mix.DynamicVars["TaintedPower"].BaseValue);
                break;
            case AcridMix:
                Set("Weak", mix.DynamicVars.Weak.BaseValue);
                Set("Vulnerable", mix.DynamicVars.Vulnerable.BaseValue);
                Set("SelfPoison", mix.DynamicVars["SelfPoison"].BaseValue);
                break;
            case SparklingMix:
                Set("Energy", mix.DynamicVars.Energy.BaseValue);
                break;
        }
    }

    private static string Key(MixKind kind) => kind.ToString().ToLowerInvariant();

    // This override drops the base title's "+", so it is added here
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
            return title.GetFormattedText() + (IsUpgraded ? "+" : "");
        }
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        var draw = new LocString("cards", "ALCHEMIST-COMPOUND_MIX.part_draw");
        draw.Add(DynamicVars["Bonus"]);
        var lines = new List<string> { draw.GetFormattedText() };
        if (!_composed) lines.Add("??????");
        else
            foreach (var kind in Doubled ? new[] { _first } : new[] { _first, _second })
                if (kind != MixKind.Zesty) lines.Add(Part(kind));
        description.Add("Body", string.Join("\n", lines));
    }

    private string Part(MixKind kind)
    {
        var part = new LocString("cards", $"ALCHEMIST-COMPOUND_MIX.part_{Key(kind)}");
        switch (kind)
        {
            case MixKind.Bursting: part.Add(DynamicVars.Damage); break;
            case MixKind.Syrupy: part.Add(DynamicVars.Block); break;
            case MixKind.Fuming:
                part.Add(DynamicVars["FumingDamage"]);
                part.Add(DynamicVars["Tainted"]);
                break;
            case MixKind.Acrid:
                part.Add(DynamicVars["Weak"]);
                part.Add(DynamicVars["Vulnerable"]);
                part.Add(DynamicVars["SelfPoison"]);
                break;
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
            case MixKind.Fuming:
                if (CombatState is not { } combat) return;
                await DamageCmd.Attack(DynamicVars["FumingDamage"].IntValue)
                    .WithHitFx(HitVfx("vfx/vfx_sandy_impact"))
                    .FromCard(this, play)
                    .TargetingAllOpponents(combat)
                    .Execute(choiceContext);
                if (play.Target is not { IsAlive: true } tainted) return;
                await PowerCmd.Apply<TaintedPower>(choiceContext, tainted, DynamicVars["Tainted"].IntValue, Owner.Creature, this);
                break;
            case MixKind.Acrid:
                if (play.Target is not { IsAlive: true } debuffed) return;
                await PowerCmd.Apply<WeakPower>(choiceContext, debuffed, DynamicVars["Weak"].IntValue, Owner.Creature, this);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, debuffed, DynamicVars["Vulnerable"].IntValue, Owner.Creature, this);
                await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
                break;
            case MixKind.Sparkling:
                await PlayerCmd.GainEnergy(DynamicVars["Energy"].BaseValue, Owner);
                break;
        }
    }
}
