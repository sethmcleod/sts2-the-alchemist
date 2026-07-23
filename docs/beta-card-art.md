# Beta card art

Every card without final art shows a placeholder gradient from
`Alchemist/images/card_portraits/beta/`. The gradients follow a system. This
document gives the rules. The generator is
[scripts/gen_beta_art.py](../scripts/gen_beta_art.py). Its `COLORS` table
holds the colors and directions; cards.csv holds everything else.

## The system

- **Every card has a unique gradient.** The COLORS table in the script
  maps each card to one color pair and one direction. No two cards share
  a pair.
- **Families share an anchor color.** Cards in the same mechanic family
  (Seep, Ferment, Gambit, and so on) share the start color and vary the
  end color. A family reads at a glance; a card stays unique. A tail hue
  can repeat across families because the anchors keep the pairs apart.
- **The csv is the source for everything else.** Card names, rarity,
  type, cost, and description come from cards.csv. A rename shows up as
  a validate error, not as silently stale art.
- **Verticals mark Basics and tokens.** Prime (Infuse family),
  Distillate, and the four magnum opus stage tokens are vertical.
  Distillate shares no color with the Card crafting family, because the
  game renders the token next to those tooltips.

## Technical notes

- Most cards use a 1000x760 canvas. Full-art cards (Aureate, Elixir) use
  606x852; the `SIZES` table in the script holds the exceptions.
- Diagonals run corner to corner for the art size, not at 45 degrees.
- The generator interpolates in float and applies ordered (Bayer)
  dithering before it quantizes to 8-bit. Without dither, a two-color
  gradient shows visible bands. The ordered pattern also compresses well,
  so each PNG stays near 100 KB.
- Output is deterministic: a rerun makes byte-identical files and a clean
  `git status`.

## Commands

```sh
python3 scripts/gen_beta_art.py            # regenerate all placeholder PNGs
python3 scripts/gen_beta_art.py sheet      # write beta-art-sheet.html, a
                                           #   compendium-style card list
python3 scripts/gen_beta_art.py validate   # check COLORS against cards.csv
```

After a regeneration, run `scripts/dev.sh publish`.

## To add a card

1. Add the card to cards.csv as usual.
2. Add one COLORS entry in `scripts/gen_beta_art.py`: reuse the family
   anchor color, pick an unused end color, pick a direction.
3. Rerun the script. The `validate` step fails on a missing card, an
   unknown card, or a duplicated color pair.

The final art replaces a placeholder when a real PNG lands in
`card_portraits/` (see [BUILD.md](../BUILD.md) for the asset paths). Do not
delete the beta file; the lint check expects one per card.
