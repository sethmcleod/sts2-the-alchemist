---
name: balance-sync
description: Apply a design pass from an updated cards.csv. The user usually puts this file in ~/Downloads/cards.csv as the authoritative design sheet. Use this skill when a new or edited CSV of card designs appears and its changes must go into the mod. Use it also for a request to "apply the balance changes", to "sync the design sheet", or to "update the cards from the CSV". This skill compares the dropped sheet with the repo, classifies every change, and applies each change through the three-way rule.
---

# Sync a design pass from a dropped cards.csv

A new `cards.csv` sometimes appears, usually in `~/Downloads/`. It is the authoritative
design update. It can contain card renames, reworks, new cards, retirements, and sometimes
new mechanics. The dropped sheet is the source of truth for the **intent**. Make the repo
agree with the sheet. Keep the three files in sync.

The columns are exactly `Card,Rarity,Type,Cost,Description`. The sheet uses plain text. It
gives the upgraded value in parentheses, as `(upgraded)`. This is the same format as the
`cards.csv` file in the repo.

## 1. Diff, do not assume

Diff the dropped sheet against the `cards.csv` file in the repo. The diff shows you what
changed. Do not assume that you know the changes from earlier in the conversation.

```sh
diff <(sort ~/Downloads/cards.csv) <(sort cards.csv)
```

Read the diff row by row. **Classify each change** before you write code:

- **Number or text change**. The card is the same. The cost, the damage, or the
  description is different.
- **Rename**. The mechanics of the card stay the same, but the name changes. A rename
  touches the class name, the file name, and the model id. It also touches all loc keys,
  the portrait filename, references from other cards, and any test scenario. A rename
  affects more files than any other type of change. Find every reference.
- **Rework**. The name stays the same, but the effect changes. A rework usually changes
  the `OnPlay` logic, not only the numbers.
- **New card**. The card is not in the repo. Use the **card** skill to add it from start
  to end.
- **Retired card**. The card is in the repo, but it is not in the sheet. Remove its class,
  its loc keys, its csv row, its portrait, and any test scenario.
- **New mechanic or keyword**. This is a rework that adds a concept that the mod does not
  have. Examples are a new keyword, a new power, or a new enchantment. This is the largest
  type of change.

## 2. Confirm intent before large changes

This project makes a plan before the work starts. A rework has a better result after a
short alignment. Do this for every change that is more than a number change. This applies
to reworks that change logic, to retirements, and to new mechanics. Summarize what you
read in the diff, then confirm the intended behavior before you write code. A question
costs less time than a correction of a wrong guess.

## 3. Apply each change through the three-way rule

Every card change touches the **code**, `cards.json`, and `cards.csv` together. The
**card** skill gives the procedure for one card. Apply that procedure to each changed
card. For a rename, first grep the repo for the old name. This makes sure that no
reference stays behind.

```sh
grep -rn "OldName" AlchemistCode Alchemist/localization cards.csv scripts/tests
```

For each changed row, the `cards.csv` file in the repo must agree exactly with the dropped
sheet. Copy the text of the sheet. Do not write your own version of it.

## 4. Verify

```sh
scripts/dev.sh lint                 # three-way sync must be clean
scripts/dev.sh publish
scripts/dev.sh test --changed       # runs only the groups your edits can affect
```

Update or add a **regression scenario** for each card with changed numbers or changed
behavior. The **card** skill gives this procedure. Use the **playtest** skill to drive the
live game. That skill applies the process rules and the save profile rules for you.

Report what changed. Group your report by the classifications above. This lets a reviewer
compare the pass against the intended design.
