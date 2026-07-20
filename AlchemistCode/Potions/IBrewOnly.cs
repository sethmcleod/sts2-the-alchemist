namespace Alchemist.AlchemistCode.Potions;

// A Brew-only potion never comes from random generation, rewards, or the shop. The potion pool
// filters these out, and BrewRestSiteOption offers them with its own weighted roll
public interface IBrewOnly;
