using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Alchemist.AlchemistCode.Cards;

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
