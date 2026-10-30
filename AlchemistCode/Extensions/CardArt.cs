namespace Alchemist.AlchemistCode.Extensions;

public sealed record CardArt(string? Final, string? Beta, string? Big, string Fallback)
{
    public bool HasFinalAndBeta => Final != null && Beta != null;

    public string Portrait(bool useBeta) => useBeta && Beta != null ? Beta : Fallback;

    public string? BigPortrait(bool useBeta) => useBeta ? null : Big;
}
