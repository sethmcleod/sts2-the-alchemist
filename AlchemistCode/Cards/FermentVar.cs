using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Alchemist.AlchemistCode.Cards;

// A card number that grows with fermentation. The per-turn growth is either a flat number or
// another var on the same card, so an upgrade to the growth rate is read live
public sealed class FermentVar : PreviewVar
{
    private readonly string? _perTurnVar;
    private readonly int _perTurnFlat;

    public FermentVar(string name, int baseValue, int perTurn) : base(name, baseValue)
    {
        _perTurnFlat = perTurn;
    }

    public FermentVar(string name, int baseValue, string perTurnVar) : base(name, baseValue)
    {
        _perTurnVar = perTurnVar;
    }

    protected override int Bonus(AlchemistCard card, Creature? target)
    {
        var turns = card.FermentTurns;
        if (turns <= 0) return 0;
        var perTurn = _perTurnVar != null ? card.DynamicVars[_perTurnVar].IntValue : _perTurnFlat;
        return perTurn * turns;
    }
}
