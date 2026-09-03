using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Relics;

public class MurkyFlask : FlaskRelic
{
    protected override int Antitoxin => 3;
    protected override int Dose => 3;

    // Without this, BaseLib falls back to Circlet for the Touch of Orobas starter upgrade
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<RadiantFlask>();
}
