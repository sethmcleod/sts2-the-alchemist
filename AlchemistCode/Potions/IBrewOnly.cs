namespace Alchemist.AlchemistCode.Potions;

// A Brew-only potion never comes from random generation, rewards, or the shop. Three things
// make that work together:
//   1. AlchemistPotionPool.GetUnlockedPotions filters these out. This is the load-bearing guard:
//      PotionFactory reads that pool, and so do the events that pick a potion straight from the
//      pool with no rarity filter (Wellspring, The Legends Were True, Battleworn Dummy,
//      Endless Conveyor)
//   2. Rarity is Event, which no rarity-filtered path can roll. PotionFactory only ever rolls
//      Common, Uncommon, or Rare
//   3. MainFile registers them into the base game's EventPotionPool. UnlockState.Potions reads
//      every pool, so this is what marks them unlocked in the compendium. No generation path
//      reads EventPotionPool, which is how the base game keeps Ambergris obtainable but ungenerated
// BrewRestSiteOption then offers them with its own weighted roll
public interface IBrewOnly;
