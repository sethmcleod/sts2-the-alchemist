---
name: getting-started
description: Orient a new contributor to The Alchemist mod. Use this skill when someone is new to the repo, new to Claude Code, or new to programming, and asks how to get set up, how the project works, where things live, or how to make their first change. Also use it when a request is vague ("help me contribute", "where do I start") and the person needs a tour before a task.
---

# Getting started with The Alchemist

This repo is a custom character mod for Slay the Spire 2. You do not need deep C#
knowledge to contribute. Most changes follow a small set of repeatable steps, and the
scripts check your work for you. This skill is the tour. Give the person the shortest
path to their goal, and explain each step in plain language as you go.

## First-time setup

Run these once. Explain what each one does before you run it.

```sh
scripts/setup.sh      # clones the tooling and installs the bridge mods
scripts/dev.sh doctor # prints a checkmark or a cross for each prerequisite
```

If `doctor` shows a cross, fix that item before anything else. The output names the
problem. [BUILD.md](../../../BUILD.md) explains the prerequisites.

## The core loop

Almost every change follows the same loop:

```sh
scripts/dev.sh lint     # fast offline check, run after every edit
scripts/dev.sh publish  # build the mod and install it into the game
```

Then look at the change in the game. The **playtest** skill drives the live game safely.
It matters because the game has real hazards (a publish over a running game breaks that
session, and a test on a real save profile changes real progress). Use that skill for
anything that touches the live game.

## Where things live

| Path | What it holds |
|---|---|
| `AlchemistCode/Cards/<Rarity>/` | one C# class per card |
| `AlchemistCode/Powers/`, `Relics/`, `Potions/` | the other game entities |
| `Alchemist/localization/eng/` | all on-card and in-game text (JSON) |
| `cards.csv` | the plain-text design sheet, one row per card |
| `Alchemist/images/` | card art, icons, character art |
| `scripts/tests/` | the regression scenarios (small JSON files) |
| `docs/` | worked examples and troubleshooting |

## The one rule that prevents most mistakes

A card lives in three files: the C# class, the localization JSON, and `cards.csv`. **If
you change one, change all three.** `scripts/dev.sh lint` checks this offline. The
**card** skill walks through the full procedure, and
[docs/adding-a-card.md](../../../docs/adding-a-card.md) is a complete worked example.

## Which skill to use next

- **card**: add a card, or change the numbers, text, or behavior of a card.
- **playtest**: run the regression suite, or check a change in the live game.
- **balance-review**: read finished run data and suggest balance changes.

## For someone new to code

Offer to explain, not just to do. When you make a change for a new contributor, show the
diff and describe what each part does in one or two sentences. Point at the matching
section of [CONTRIBUTING.md](../../../CONTRIBUTING.md) so they can read the rule behind
the change. Small, complete, verified changes teach more than large ones.
