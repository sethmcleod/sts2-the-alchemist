using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Alchemist.AlchemistCode;

// One definition of the Gambit line. It used to be copy-pasted in six places, in two different forms
// (an integer comparison and a double percentage), across two cards, three powers, and the keyword
// check. Moving the threshold meant editing all six, and missing one left a card silently disagreeing
// with the keyword printed on its own face
public static class Gambit
{
    // A third of max HP or less. Deep enough to be one strong hit from death, so entering it is a real
    // decision rather than a state you drift into and never leave
    public static bool IsActive(Creature? creature) =>
        creature is { MaxHp: > 0 } c && c.CurrentHp * 3 <= c.MaxHp;
}
