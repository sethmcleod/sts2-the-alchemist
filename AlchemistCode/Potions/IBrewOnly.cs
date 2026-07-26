namespace Alchemist.AlchemistCode.Potions;

// A Brew-only potion never comes from random generation, rewards, or the shop. Three things together:
//   1. AlchemistPotionPool.GetUnlockedPotions filters them out. This is the load-bearing guard, since
//      PotionFactory reads that pool, as do the events that pick from it with no rarity filter
//   2. Rarity is Event, which no rarity-filtered path can roll
//   3. MainFile registers them into EventPotionPool. UnlockState.Potions reads every pool, so this is
//      what marks them unlocked in the compendium, and no generation path reads it
// BrewRestSiteOption then offers them with its own weighted roll
public interface IBrewOnly;
