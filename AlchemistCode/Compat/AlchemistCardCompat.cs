// COMPAT-BRANCH: any

namespace Alchemist.AlchemistCode.Cards;

// A played Ferment card goes to the Discard like any other card, and the Residue it adds is the whole
// cost of the play. Until 2026-08-18 this file took the card out of combat on play (beta: a
// PileType.None result location; main: an Exhaust), which made a Ferment deck a one-shot per fight.
// Kept as a partial so the main branch's copy can be replaced by this file on the next promote.
public abstract partial class AlchemistCard
{
}
