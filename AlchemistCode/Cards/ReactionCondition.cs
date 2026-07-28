namespace Alchemist.AlchemistCode.Cards;

// What the card played immediately before this one, must have been for a Reaction to trigger
public enum ReactionCondition
{
    None,
    Attack,
    Skill,
    Power,
    Exhaust,
    Block,
    Enchanted,
}
