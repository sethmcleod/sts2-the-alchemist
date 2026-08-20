using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Relics;

public class WeatheredKit : KitRelic
{
    protected override int Antitoxin => 8;
    protected override int Dose => 1;

    // Without this, BaseLib falls back to Circlet for the Touch of Orobas starter upgrade
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<GildedKit>();
}
