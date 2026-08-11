# How to release The Alchemist

This document tells you how to make a version and how to release it. The flow is
one command, `scripts/dev.sh release`. You then run a git push by hand. Two rules
are the most important:

- Every player-visible change gets a `CHANGELOG.md` entry.
- Each version section in the changelog is the Steam Workshop update note for
  that version.

## Two branches, two Workshop items

Slay the Spire 2 has a default branch and a `public-beta` branch, and their game
DLLs differ enough that one build cannot serve both. A mod compiled against the
wrong one fails to load with a `ReflectionTypeLoadException`. Thus the mod ships
twice:

| Branch | Game branch    | Workshop item                | Tags          | GitHub Release | Cadence                     |
| ------ | -------------- | ---------------------------- | ------------- | -------------- | --------------------------- |
| `beta` | `public-beta`  | The Alchemist (Beta Branch)  | `vX.Y.Z-beta` | pre-release    | fast, this is where you work |
| `main` | default        | The Alchemist                | `vX.Y.Z`      | Latest         | slow, promoted from beta     |

`workshop/targets.json` holds this map. It is **identical on both branches** on
purpose: it is the single file that knows about both, so a merge between the
branches never has to resolve it. Every `scripts/dev.sh` command reads the branch
you are on and picks the item, the tag, and the pre-release flag from it. There is
nothing to pass and nothing to remember.

Day to day you work on `beta`. `main` moves only when you promote.

## Version policy

The version is in one location only: `Alchemist.json` (`"version": "vX.Y.Z"`).
Only `scripts/dev.sh release` changes it. This project follows
[Semantic Versioning](https://semver.org).

**Both branches share one version line.** `beta` moves it forward; `main` inherits
whatever version the merge brought over and ships it unchanged. So `v0.7.0-beta`
and `v0.7.0` are the same content, and the `-beta` suffix is a real semver
pre-release, which means `v0.7.0-beta` sorts *before* `v0.7.0` — the order the two
actually ship in. The alternative, an independent version line per branch, makes
every merge fight over `Alchemist.json` and forces you to explain why beta 0.9 is
older than main 1.0.

Three rules keep the numbers unambiguous:

- **beta bumps** with `patch`, `minor`, or `major`, by the table below.
- **main promotes** with `scripts/dev.sh release promote`, which keeps the merged
  version rather than inventing a new one.
- **A main-only fix** (something that makes the default branch work and does not
  belong on beta) bumps `patch` on `main`. Keep beta ahead so the numbers never
  describe two different things; promoting by `minor` gives you the room.

For a game mod, read the semver rules as follows:

| Increase                                  | When to use it                                                                                                                        | Examples                                                                            |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| **PATCH** (`0.1.0 → 0.1.1`)               | Balance changes, bug fixes, and fixes to text, tooltips, or art. This increase adds no content and removes no content.                | Change the cost of a card. Fix a Ferment interaction. Change the text of a keyword. |
| **MINOR** (`0.1.0 → 0.2.0`)               | New content, or more mechanics that do not break the current saves.                                                                   | Add cards, relics, or potions. Add an epoch. Add a keyword.                         |
| **MAJOR** (`0.x → 1.0`, then `1.x → 2.0`) | Changes that break the saves, or changes to the identity of the mod. **`1.0.0` is kept for the first public Steam Workshop release.** | Remove or rename large card sets. Change a core mechanic completely.                |

Before version 1.0, the mod is feature-complete, but it has no public release.
Thus, use MINOR or PATCH. Keep MAJOR for the 1.0 Workshop release.

Two related fields in the manifest:

- **`min_game_version`**: change this field by hand when a release needs a newer
  Slay the Spire 2 build.
- **BaseLib `min_version`**: do **not** change this field by hand. The build sets
  it automatically from the BaseLib version that you build against
  (`Alchemist.csproj`, `UpdateDependencyVersions`).

## The changelog (hybrid workflow)

You write `CHANGELOG.md` by hand in the
[Keep a Changelog](https://keepachangelog.com) format. The
[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) give you
the first draft:

1. Write the commit messages with Conventional Commit prefixes (`feat:`, `fix:`,
   `refactor:`, …). A bad commit message gives a bad changelog draft.
2. Before a release, run `scripts/dev.sh changelog`. The command reads the
   commits after the last tag. It prints a draft in groups (Added / Fixed /
   Changed / Other). The command writes no files.
3. Paste the applicable lines below `## [Unreleased]`. Then **write these lines
   again in language for players**. For example, "feat: rework infuse
   enchantments" becomes "Infuse now grants type-matched enchantments that
   stack." Remove the lines that apply only to development (build, ci, test, and
   most chores). Put each line in one of these Keep a Changelog sections: Added,
   Changed, Deprecated, Removed, Fixed, Security.

The `## [Unreleased]` section that you write is the release note. The `release`
command does not run when the `## [Unreleased]` section is empty.

### Entry style (match the official STS2 patch notes)

Each entry must read like a line from a Mega Crit patch note. Study the real
notes for the current voice: they ship in the game at
`res://localization/eng/patch_notes/<date>.md` (extract with
`extract_game_assets`). The rules below hold as of game v0.109.0.

- **Lead with a past-tense verb**, then the entity and its kind, then the change:
  - **Buffed** X card/relic/potion: `<stat>` increased from `A -> B`
  - **Nerfed** X card/relic/potion: `<stat>` decreased from `A -> B`
  - **Reworked** X card: `"old text" -> "new text"`
  - **Changed** X card: use for rarity swaps, upgrade-path changes, or a mix of
    a buff and a nerf that is neither on the whole
  - **Added** / **Removed** / **Renamed** / **Moved** for those operations
- **Buffed/Nerfed name the direction; the arrow shows the numbers.** Write
  "Buffed X: damage increased from 6 -> 9", not "Increased X damage 6 -> 9". The
  direction word must match the player's benefit, not the number: a lower
  requirement is a **buff** ("free-cost condition decreased from 7 -> 5").
- **Use the `A -> B` arrow** for every value change. For upgraded cards, the base
  and upgraded values ride together: `50% (75%) -> 75% (100%)`.
- **Quote new or reworked card text verbatim**, in the same wording the card
  shows in game (see [CONTRIBUTING.md](CONTRIBUTING.md) for the text rules).
- **One line per change.** Split a compound change into nested bullets rather
  than a long sentence. Keep development detail and UI micro-copy out.
- **Do not bold entity names.** The in-game notes wrap them in `[b]...[/b]`; this
  file is plain Keep a Changelog markdown, and the released sections do not bold.

A released version section is the published Steam Workshop note for that version.
Do not rewrite a tagged section's meaning after release; a stylistic touch-up is
fine, but update the Workshop note too if you have already posted it.

## How to cut a beta release

This is the usual one. Be on the `beta` branch with the game on `public-beta`.

```sh
scripts/dev.sh changelog        # draft; curate ## [Unreleased] in CHANGELOG.md by hand
scripts/dev.sh release minor    # or: patch | major | an explicit X.Y.Z
                                # examine the diff, then:
scripts/dev.sh publish-release  # commit, tag vX.Y.Z-beta, push, and put it on GitHub
```

## How to promote beta into main

Do this when a batch of beta releases has settled and you want the default-branch
players to get it. It is infrequent by design.

```sh
git switch main
scripts/dev.sh sync-main        # merge beta into main (never a rebase; see below)
                                # switch Steam to the default branch, then:
scripts/dev.sh publish          # rebuild against the default-branch DLL
                                # play it; confirm it loads at all (see BUILD.md)
scripts/dev.sh release promote  # keep the merged version, package it
scripts/dev.sh publish-release  # commit, tag vX.Y.Z, push, Latest on GitHub
```

`sync-main` merges with `--no-ff` and refuses to rebase. This is not a style
preference: public tags point at commits on both branches, and the release zip
attached to each GitHub Release describes that exact history. A rebase moves those
commits, and every tag then names a commit that no longer exists.

If the merge conflicts, take beta's side for content and keep main's side for
anything main fixed on its own. `workshop/targets.json` never conflicts, which is
the whole reason it exists.

Three files have a known resolution:

| File | Resolution |
| ---- | ---------- |
| `AlchemistCode/Compat/GameCompat.cs` | **keep main's.** The method surface is identical on both sides; only the bodies differ. If beta added a method, copy that method across and write main's body for it. |
| `AlchemistCode/Compat/SepsisPowerCompat.cs` | **keep main's**, for the same reason. |
| `workshop/workshop.json` | take beta's. The `[quote]` banner at the top will be beta's, which is wrong for main, but the next `release promote` rebuilds it from `targets.json`, so it heals itself. |

`workshop/mod_id.txt` is not tracked: it holds a different item id per branch, and
`release` writes it from `targets.json` anyway. `--id` on the upload command is
what actually decides the target, so the file is only a convenience. The first
merge after it was untracked on `beta` reports a delete/modify conflict on `main`;
resolve it by deleting the file (`git rm workshop/mod_id.txt`), and it never
recurs.

The `release` command (see `do_release` in `scripts/dev.sh`) does these steps:

1. **Preflight**: the command makes these checks first:
   - The working tree must be clean.
   - The current branch must have a target in `workshop/targets.json`, and that
     target must name a Workshop item.
   - The installed game must be on the Steam branch that this git branch targets.
     This check is the one that stops you from shipping a build that cannot load.
   - The notes must not be empty. A normal release reads `## [Unreleased]`; a `promote` reads
     `## [X.Y.Z]` instead, because its notes came over in the merge already rolled under that
     heading, which leaves `## [Unreleased]` correctly empty.

   The command then runs `dotnet build` and the `lint` check. Play the current
   build before a release; the command does not verify the mod against the live
   game for you.

2. **Compute**: the command calculates the new version from the keyword
   (`patch`, `minor`, or `major`). You can also give an explicit `X.Y.Z` value.
   The command makes sure that the new version is greater than the current
   version. `promote` is the exception: it keeps the version, and instead checks
   that the tag does not exist yet.
3. **Update**: the command changes the `version` field in `Alchemist.json`. In
   `CHANGELOG.md`, it replaces `## [Unreleased]` with `## [X.Y.Z] - <date>`. It
   then adds a new, empty Unreleased section. A `promote` skips this step, because
   the merge already brought beta's rolled changelog across.
4. **Build and package**: the command runs `dotnet publish`. It then writes two
   files, both named for the tag, so the two branches never overwrite each other
   in `dist/`:
   - `dist/Alchemist-<tag>.zip`, the file that players install (see below).
   - `dist/RELEASE_NOTES-<tag>.txt`, the changelog section for this version, with
     each bullet on one line because both paste targets wrap text themselves. Use
     it for the GitHub Release body and for the Workshop update note.
   It also points `workshop/` at the right item: `workshop/mod_id.txt` and the
   `title` in `workshop/workshop.json` are both written from
   `workshop/targets.json`, so a `ModUploader upload -w workshop` afterwards
   cannot go to the wrong place.
5. **Stop and print**: the command stops so that you can examine the diff. It
   commits nothing. When the diff is correct, run `publish-release`.

### `scripts/dev.sh publish-release`

This command does the last mile. It needs the [GitHub CLI](https://cli.github.com)
(`brew install gh`, then `gh auth login` one time). `gh` reads the repository from
the `origin` remote and holds the token, so this repository stores no secret.

The command does these steps:

1. **Commit**: if the only pending changes are `Alchemist.json` and
   `CHANGELOG.md` (what `release` writes), it commits them as
   `release: vX.Y.Z`. Any other pending change stops the command.
2. **Tag**: it puts the annotated tag `vX.Y.Z` on the tip commit. A tag that
   already points at another commit moves only with `--force`.
3. **Push**: it pushes `main` and the tag. With `--force` it uses
   `--force-with-lease`, which stops if the remote holds a commit that your
   clone has never seen.
4. **Publish**: it creates the GitHub Release, or updates it when the release
   is already there. The title is the tag, the body is
   `dist/RELEASE_NOTES-vX.Y.Z.txt`, and the zip goes up as the asset
   (`--clobber` replaces an asset of the same name). The release is a full
   release, not a pre-release.

Options: `--force` (move a public tag after a history rewrite), `--draft`
(publish the release as a draft), and an explicit `vX.Y.Z` to override the
version from the manifest.

> [!CAUTION]
> `--force` rewrites public history. Use it only for a release that is yours
> alone and that nobody has built on. `git push --force-with-lease` protects
> you from overwriting a commit that you have not seen, but it does not protect
> a person who already pulled the old history.

Git ignores the `dist/` folder. You build these files again for each release. Do
not commit them.

## How players install it

**The Steam Workshop is the best method to install the mod and to play it.** The
installation is one click. The Workshop updates the mod automatically. It also
installs the **BaseLib** dependency automatically.

> [!NOTE]
> The Workshop release is not available yet. It waits for the character artwork.
> Until then, use the manual zip below.

**Manual installation (temporary, from a GitHub Release zip):**

1. Install [**BaseLib**](https://github.com/Alchyr/BaseLib-StS2) first. The
   Alchemist mod needs BaseLib.
2. Download `Alchemist-vX.Y.Z.zip`. Extract the `Alchemist/` folder into the
   `mods/` folder of your game:
   - **macOS**: `…/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/`
   - **Windows/Linux**: the `mods/` folder in the same location as the game
     executable.
3. Make sure that your game version is `min_game_version` from the manifest or
   higher. Then start the game.

The zip contains only the files that the game loads: `Alchemist.dll`,
`Alchemist.json`, and `Alchemist.pck`. These files are in one top-level
`Alchemist/` folder. You do not need a clone of the repo. You do not need .NET or
Godot. Developers who build from source use `scripts/dev.sh publish` instead.
See [BUILD.md](BUILD.md).

## Uploading to Steam

The upload is a separate manual step. `release` and `publish-release` never touch
Steam; `release` prints the exact command to run, with the item id already filled
in from `workshop/targets.json`:

```sh
cd ~/code/sts2-mod-uploader && DOTNET_ROLL_FORWARD=LatestMajor \
  dotnet run -c Release --no-build -- upload --id <itemId> -w ~/code/sts2-the-alchemist/workshop
```

**Always pass `--id`.** The uploader picks its target in this order: `--id`, then
`workshop/mod_id.txt`, and **if neither is present it creates a brand new Workshop
item**. With two items in play, an upload that resolves the wrong way either
overwrites the other branch's item (including its title) or spawns a duplicate.
Naming the id makes that impossible.

`workshop/mod_id.txt` is no longer tracked by git, because it now differs per
branch and would conflict on every merge. `scripts/dev.sh release` writes it for
the branch you are on, so it is correct locally; `--id` is the belt to its braces.

### Creating the main-branch Workshop item

There is no way to create an item from the Steam UI. The uploader makes one, but
only as a side effect of an upload with no target — which is also the footgun
above, so do this deliberately and once:

1. Be on `main`, with the game on its default branch, and `scripts/dev.sh publish`
   already run so `workshop/content/` holds a build that actually loads there.
2. Set `"visibility": "private"` in `workshop/workshop.json`. A new item starts
   empty; make it public only after you have checked the page.
3. Set `"title"` to `The Alchemist`. (`release` writes this from `targets.json`,
   but you have not run `release` yet at this point.)
4. **Delete `workshop/mod_id.txt`** and pass no `--id`. This is the one time you
   want the uploader to create rather than update. Confirm the file is gone:
   `ls workshop/mod_id.txt` must fail.
5. Run the uploader with `-w` only. It logs `Creating new workshop item...`, then
   writes the new id into `workshop/mod_id.txt`.
6. Copy that id into `workshop/targets.json` under `main.itemId`, and commit. From
   here on, every upload names the item with `--id` and this never repeats.

The `new` subcommand does **not** do any of this — it copies a local template
folder and never contacts Steam. Do not reach for it.

Then rename the existing item 3780726901 to *The Alchemist (Beta Branch)*, which
happens by itself on the next beta `release` + upload, since the title now comes
from `targets.json`. Cross-link the two descriptions so a player on the wrong
branch can find the right item.

## Steam Workshop fields (for the 1.0 launch)

When you publish the mod, each Workshop field has a source:

| Workshop item             | Source                                                                        |
| ------------------------- | ----------------------------------------------------------------------------- |
| Update note (per version) | the `CHANGELOG.md` section for that version, or `dist/RELEASE_NOTES-*.txt`    |
| Preview image             | `Alchemist/mod_image.png` (already in the `.pck`)                             |
| Description               | the `description` field in `Alchemist.json`, plus the main points from README |
| Dependency                | **BaseLib**. List it, and the Workshop then installs it automatically.        |

The update note drops its markdown headings on the way into `workshop.json`:
Steam renders BBCode, so a `### Fixed` would show the hashes. `CHANGELOG.md` and
the GitHub release body keep theirs.
