---
name: balance-sync
description: Apply a design pass from an updated cards.csv, typically one dropped in ~/Downloads/cards.csv as the authoritative design sheet. Use this whenever a new or edited CSV of card designs appears and its changes need to land in the mod, or when asked to "apply the balance changes", "sync the design sheet", or "update the cards from the CSV". Diffs the dropped sheet against the repo, classifies every change, and walks each one through the three-way rule.
---

# Sync a design pass from a dropped cards.csv

A fresh `cards.csv` sometimes appears (usually in `~/Downloads/`) as the authoritative
design update: card renames, reworks, new cards, retirements, and occasionally whole new
mechanics. The dropped sheet is the source of truth for **intent**. Your job is to make the
repo match it, correctly and in sync.

The columns are exactly `Card,Rarity,Type,Cost,Description`, plain text with `(upgraded)`
in parentheses. This is the same format as the repo's own `cards.csv`.

## 1. Diff, do not assume

Diff the dropped sheet against the repo's `cards.csv` to find what actually changed. Do not
assume you already know from earlier in the conversation:

```sh
diff <(sort ~/Downloads/cards.csv) <(sort cards.csv)
```

Read the diff row by row and **classify each change** before touching code:

- **Number/text tweak**: same card, different cost/damage/description.
- **Rename**: a card's mechanics are unchanged but its name changed. This touches the class
  name, file name, model id, all loc keys, the portrait filename, references from other
  cards, and any test scenario. Renames have the widest blast radius; find every reference.
- **Rework**: same name, changed effect. Often changes the `OnPlay` logic, not just numbers.
- **New card**: not in the repo yet. Use the **card** skill to add it end to end.
- **Retired card**: in the repo, gone from the sheet. Remove its class, loc keys, csv row,
  portrait, and any test scenario.
- **New mechanic / keyword**: a rework that introduces a concept the mod doesn't have yet
  (a new keyword, power, or enchantment). This is the largest kind of change.

## 2. Confirm intent before large changes

This project plans before building, and reworks land better after a quick alignment. For
anything beyond number tweaks, especially reworks that change logic, retirements, or a new
mechanic, summarize what you read from the diff and confirm the intended behavior before
writing code. A clarifying question is cheaper than unwinding a wrong guess.

## 3. Apply each change through the three-way rule

Every card change touches **code + `cards.json` + `cards.csv`** together. The **card** skill
is the procedure for a single card; apply it per changed card. For a rename, grep the repo
for the old name first so no reference is left dangling:

```sh
grep -rn "OldName" AlchemistCode Alchemist/localization cards.csv scripts/tests
```

The repo's `cards.csv` should end up matching the dropped sheet exactly for the rows that
changed. Copy the sheet's wording rather than paraphrasing it.

## 4. Verify

```sh
scripts/dev.sh lint                 # three-way sync must be clean
scripts/dev.sh publish
scripts/dev.sh test --changed       # runs only the groups your edits can affect
```

Update or add a **regression scenario** for every card whose numbers or behavior changed
(the **card** skill covers this). Drive the live game with the **playtest** skill so the
process and save-profile safety rules are handled for you.

Report back what changed, grouped by the classification above, so the pass can be reviewed
against the intended design.
