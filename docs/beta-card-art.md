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
- **Direction shows the card type.** In the Common, Uncommon, and Rare
  pool: Attacks ascend (bottom left to top right), Skills descend (top
  left to bottom right), and Powers run left to right, with the dark end
  on either side to keep some variety. A player can tell the type from
  the art alone.
- **Verticals mark Basics and tokens.** Prime, Distillate, the four
  magnum opus stage tokens, and Foul Vapor are vertical, dark at the
  top. Distillate shares no color with the Card crafting family, because
  the game renders the token next to those tooltips.
- **The 7 Epoch images are generated too.** The `EPOCHS` table maps each
  chapter to a pair that matches its story. All seven ascend toward the
  top right, because the timeline is a climb.

## Technical notes

- The canvas is half the display size (500x380; full-art cards use
  303x426 through the `SIZES` table). A linear gradient upscales exactly,
  so the smaller canvas costs no quality and keeps the pck small.
- The art imports LOSSLESS. Lossy WebP, which the project uses by
  default, flattens a smooth gradient into wide plateaus with visible
  steps, and raising the quality does not fix it (the banding comes from
  chroma subsampling, not the quality setting). The generator rewrites
  `compress/mode` in each .import, so a new card cannot regress by
  accident. Measured on the 97 cards plus 7 epochs: lossy 1.4 MB with
  bad banding, lossless at full res 16.2 MB, lossless at half res
  4.3 MB and pixel-identical to the source.
- Diagonals run corner to corner for the art size, not at 45 degrees.
- The generator interpolates in float and applies ordered (Bayer)
  dithering before it quantizes to 8-bit. Without dither, a two-color
  gradient shows visible bands. The ordered pattern also compresses well,
  so each PNG stays near 25 KB.
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
