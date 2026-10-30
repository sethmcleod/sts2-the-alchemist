using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Config;

namespace Alchemist.AlchemistCode.Config;

public static class BetaArtChoices
{
    private static string? _parsedFrom;
    private static HashSet<string> _exceptions = new();

    private static HashSet<string> Exceptions
    {
        get
        {
            var saved = AlchemistModConfig.BetaArtExceptions ?? "";
            if (saved == _parsedFrom) return _exceptions;
            _parsedFrom = saved;
            _exceptions = saved.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet();
            return _exceptions;
        }
    }

    public static bool ShowsBeta(string cardId) => AlchemistModConfig.UseBetaArt != Exceptions.Contains(cardId);

    public static bool Choose(string cardId, bool beta)
    {
        var exceptions = Exceptions;
        var changed = beta == AlchemistModConfig.UseBetaArt ? exceptions.Remove(cardId) : exceptions.Add(cardId);
        if (!changed) return false;
        _parsedFrom = AlchemistModConfig.BetaArtExceptions = string.Join(',', exceptions.Order());
        ModConfig.SaveDebounced<AlchemistModConfig>();
        return true;
    }

    public static void Clear() => AlchemistModConfig.BetaArtExceptions = "";
}
