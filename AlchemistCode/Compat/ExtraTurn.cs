using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Compat;

/// <summary>
/// TEMPORARY. Delete this whole folder's extra-turn support once the game's default branch has
/// caught up to the beta.
/// </summary>
/// <remarks>
/// The beta has <c>AmbergrisPower</c>, the base game's invisible extra-turn counter. The default
/// branch does not have it yet, though it does have the hooks the power is built on. Rather than
/// let the two branches diverge, BOTH now grant an extra turn through this one method and the
/// mod's own <see cref="ExtraTurnPower"/>, which compiles against either branch.
///
/// TO REMOVE, when the branches converge (MegaCrit merges public-beta into public):
///   1. change the body below to
///        PowerCmd.Apply&lt;AmbergrisPower&gt;(choiceContext, owner, 1m, owner, source)
///      and add `using MegaCrit.Sts2.Core.Models.Powers;`
///   2. delete Powers/ExtraTurnPower.cs
///   3. delete the three ALCHEMIST-EXTRA_TURN_POWER keys from localization/eng/powers.json
///   4. delete images/powers/extra_turn_power.png and images/powers/big/extra_turn_power.png
/// The three callers never change, because they only ever name this method.
/// </remarks>
public static class ExtraTurn
{
    /// <summary>Grants the owner one extra turn after the current one.</summary>
    public static Task Grant(PlayerChoiceContext choiceContext, Creature owner, CardModel? source) =>
        PowerCmd.Apply<ExtraTurnPower>(choiceContext, owner, 1m, owner, source);
}
