"""Page-sized copies of the mod's images for the stats page: the card art, the relic, potion, power
and badge icons, the character head, the energy icon and the Steam Workshop previews.

mod_meta.py names each image by its path in the repo. write() saves a WebP copy next to the data
(docs/analytics/data/img/, gitignored) and puts the copy's path, relative to the data folder, in
its place. The nightly job deploys the copies with the page, so the page serves its own images and
none of them is committed. The card frames are the exception: they come from the game, and this
page keeps a committed copy in docs/analytics/img/frames/.
"""

from pathlib import Path

from PIL import Image

import mod_meta

# The page draws card art at most about 210 px wide, so twice that stays sharp on a dense screen
ART_WIDTH = 420
ICON_WIDTH = 96
# A Workshop preview shows in the header's gallery, and full size when it is opened
PREVIEW_WIDTH = 880
PREVIEW_FULL_WIDTH = 1600


def save(source: str | None, out: Path, folder: str, width: int, cache: dict) -> str | None:
    """Write one image as WebP, no wider than width, and return its path under out."""
    if not source:
        return None
    key = f"{folder}/{source}"
    if key not in cache:
        img = Image.open(mod_meta.REPO / source)
        img = img.convert("RGBA")
        if img.width > width:
            img = img.resize((width, round(img.height * width / img.width)), Image.LANCZOS)
        # Two folders can hold the same file name (a relic and a potion), so the folder is kept
        target = out / "img" / folder / f"{Path(source).parent.name}-{Path(source).stem}.webp"
        target.parent.mkdir(parents=True, exist_ok=True)
        img.save(target, "WEBP", quality=82, method=6)
        cache[key] = target.relative_to(out).as_posix()
    return cache[key]


def write(summary: dict, out: Path) -> int:
    """Copy every image summary.json names and point it at the copy. Returns how many were written."""
    cache: dict[str, str] = {}
    for card in summary["card_info"].values():
        card["art"] = save(card.get("art"), out, "cards", ART_WIDTH, cache)
    summary["icons"] = {entry: save(path, out, "icons", ICON_WIDTH, cache)
                        for entry, path in summary["icons"].items()}
    for table in ("relic_info", "potion_info", "power_info"):
        for item in summary[table].values():
            item["icon"] = save(item.get("icon"), out, "icons", ICON_WIDTH, cache)
    for badge in summary["meta"]["badges"]:
        badge["icon"] = save(badge.get("icon"), out, "icons", ICON_WIDTH, cache)
    for preview in summary["meta"]["previews"]:
        preview["image"] = save(preview["image"], out, "previews", PREVIEW_WIDTH, cache)
        preview["full"] = save(preview["full"], out, "previews/full", PREVIEW_FULL_WIDTH, cache)
    assets = summary["meta"]["assets"]
    for name in assets:
        assets[name] = save(assets[name], out, "icons", ICON_WIDTH, cache)
    return len(cache)
