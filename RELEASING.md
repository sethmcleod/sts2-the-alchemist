# How to release The Alchemist

This document tells you how to make a version and how to release it. The flow is
one command, `scripts/dev.sh release`. You then run a git push by hand. Two rules
are the most important:

- Every player-visible change gets a `CHANGELOG.md` entry.
- Each version section in the changelog is the Steam Workshop update note for
  that version.

## Version policy

The version is in one location only: `Alchemist.json` (`"version": "vX.Y.Z"`).
Only `scripts/dev.sh release` changes it. The git tags use the same value
(`vX.Y.Z`). This project follows [Semantic Versioning](https://semver.org). For a
game mod, read the rules as follows:

| Increase | When to use it | Examples |
|---|---|---|
| **PATCH** (`0.1.0 → 0.1.1`) | Balance changes, bug fixes, and fixes to text, tooltips, or art. This increase adds no content and removes no content. | Change the cost of a card. Fix a Ferment interaction. Change the text of a keyword. |
| **MINOR** (`0.1.0 → 0.2.0`) | New content, or more mechanics that do not break the current saves. | Add cards, relics, or potions. Add an epoch. Add a keyword. |
| **MAJOR** (`0.x → 1.0`, then `1.x → 2.0`) | Changes that break the saves, or changes to the identity of the mod. **`1.0.0` is kept for the first public Steam Workshop release.** | Remove or rename large card sets. Change a core mechanic completely. |

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

## How to cut a release

```sh
scripts/dev.sh changelog        # draft; curate ## [Unreleased] in CHANGELOG.md by hand
scripts/dev.sh release minor    # or: patch | major | an explicit X.Y.Z
```

The `release` command (see `do_release` in `scripts/dev.sh`) does these steps:

1. **Preflight**: the command makes these checks first:
   - The working tree must be clean.
   - The current branch must be `main`.
   - The `## [Unreleased]` section must not be empty.

   The command then runs `dotnet build`, the `lint` check, and the regression
   suite. The suite drives the live game, thus Steam must run. The `--skip-tests`
   option disables the suite and prints a warning. The release is then not
   verified against the game. Use `--skip-tests` only when the game cannot run.
   Do not use it when the regression suite fails.
2. **Compute**: the command calculates the new version from the keyword
   (`patch`, `minor`, or `major`). You can also give an explicit `X.Y.Z` value.
   The command makes sure that the new version is greater than the current
   version.
3. **Update**: the command changes the `version` field in `Alchemist.json`. In
   `CHANGELOG.md`, it replaces `## [Unreleased]` with `## [X.Y.Z] - <date>`. It
   then adds a new, empty Unreleased section.
4. **Build and package**: the command runs `dotnet publish`. It then writes two
   files:
   - `dist/Alchemist-vX.Y.Z.zip`, the file that players install (see below).
   - `dist/RELEASE_NOTES-vX.Y.Z.txt`, the changelog section for this version, with
     each bullet on one line because both paste targets wrap text themselves. Use
     it for the GitHub Release body and for the Workshop update note.
5. **Stop and print**: the command stops. It prints the git commands below. You
   must run these commands yourself. The command does not commit, tag, or push
   anything for you.

   ```sh
   git add Alchemist.json CHANGELOG.md
   git commit -m "release: vX.Y.Z"
   git tag -a vX.Y.Z -m "vX.Y.Z"
   git push --follow-tags
   ```

Then create a GitHub Release for the tag. Attach the zip to the release. Paste
the notes into the release body.

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

## Steam Workshop fields (for the 1.0 launch)

When you publish the mod, each Workshop field has a source:

| Workshop item | Source |
|---|---|
| Update note (per version) | the `CHANGELOG.md` section for that version, or `dist/RELEASE_NOTES-*.txt` |
| Preview image | `Alchemist/mod_image.png` (already in the `.pck`) |
| Description | the `description` field in `Alchemist.json`, plus the main points from README |
| Dependency | **BaseLib**. List it, and the Workshop then installs it automatically. |

The tools do not do the upload step yet. Add the steps to this document when the
tools do the upload.
