#!/usr/bin/env bash
# Dev helper for the Alchemist mod. One command does each repeated loop.
#
#   scripts/dev.sh publish        build → godot import → publish → verify pck   (the safe default)
#   scripts/dev.sh publish-fast   build → publish → verify pck                  (code only, no import)
#   scripts/dev.sh import         godot --headless --import only
#   scripts/dev.sh lint           static check of the three-way rule (offline, no game)
#   scripts/dev.sh changelog      draft CHANGELOG entries from the commits since the last tag
#                                 (it prints only, it writes nothing)
#   scripts/dev.sh release <patch|minor|major|X.Y.Z|promote>
#                                 increase the version, roll the CHANGELOG, build, and package
#                                 dist/Alchemist-<tag>.zip (see RELEASING.md). "promote" keeps the
#                                 version it has, which is what main does after a merge from beta
#   scripts/dev.sh publish-release [--force] [--draft] [X.Y.Z]
#                                 commit the release edit, tag it, push it, and create or update
#                                 the GitHub Release with the zip and the notes. --force moves a
#                                 tag that is already public (a history rewrite)
#   scripts/dev.sh sync-main      merge beta into main so main can promote (see RELEASING.md)
#   scripts/dev.sh doctor         check every prerequisite and print ✓/✗ with the fixes
#   scripts/dev.sh env            print the resolved paths and exit
#
# ── two branches, two Workshop items ────────────────────────────────────
# beta  tracks the game's public-beta branch, releases often, and publishes to the "(Beta Branch)"
#       Workshop item. Its tags are vX.Y.Z-beta and its GitHub Releases are pre-releases.
# main  tracks the game's default branch, releases slowly, and publishes to the plain Workshop
#       item. Its tags are vX.Y.Z and its GitHub Releases are the ones GitHub calls "Latest".
# workshop/targets.json maps the branch to the item. Every command below reads the branch you are
# on, so there is nothing to pass and nothing to remember.
#
# The reason for this script: every publish needs PATH and DOTNET_ROLL_FORWARD set, plus 3 or 4
# commands in a chain. This script does all of that, so the inner loop is one word.
#
# You can override every path below with an environment variable (see the env output). The
# defaults detect the platform, so a fresh clone works and you do not have to edit this file.
set -euo pipefail

# ── the environment that every command needs ────────────────────────────
# dotnet is often not on the default login PATH. RollForward=Major lets the net9 analyzers run on
# newer runtimes.
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet/tools"
export DOTNET_ROLL_FORWARD=Major

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Game install dir: the environment override, or the default Steam library of the platform.
if [ -z "${STS2_GAME_DIR:-}" ]; then
  case "$(uname -s)" in
    Darwin) STS2_GAME_DIR="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2" ;;
    Linux)  STS2_GAME_DIR="$HOME/.steam/steam/steamapps/common/Slay the Spire 2" ;;
    *)      STS2_GAME_DIR="C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2" ;;
  esac
fi
export STS2_GAME_DIR

# Mods dir: in the .app bundle on macOS, next to the executable on the other platforms.
if [ -d "$STS2_GAME_DIR/SlayTheSpire2.app" ]; then
  GAME_MODS="$STS2_GAME_DIR/SlayTheSpire2.app/Contents/MacOS/mods"
else
  GAME_MODS="$STS2_GAME_DIR/mods"
fi

GODOT="${GODOT:-/Applications/MegaDot.app/Contents/MacOS/Godot}"
PCK="$GAME_MODS/Alchemist/Alchemist.pck"

# The lint needs Python 3.10 or later. Use a system python if the PATH has one. If the PATH
# has none, use uv. uv supplies a correct Python for you (see BUILD.md). PY_CMD holds the
# interpreter command as an array, because the uv form has more than one word.
PY_CMD=()
find_python() {
  local p
  for p in python3.13 python3.12 python3.11 python3.10 python3; do
    if command -v "$p" >/dev/null && "$p" -c 'import sys; sys.exit(0 if sys.version_info >= (3,10) else 1)' 2>/dev/null; then
      PY_CMD=("$p"); return 0
    fi
  done
  if command -v uv >/dev/null 2>&1; then
    PY_CMD=(uv run --no-project --python 3.12 python); return 0
  fi
  return 1
}
find_python || true
have_py() { [ "${#PY_CMD[@]}" -gt 0 ]; }
no_py_msg="no Python 3.10 or later found; install uv (https://astral.sh/uv) and it will supply one, or install Python directly"

step() { printf '\n\033[1;36m▶ %s\033[0m\n' "$*"; }
ok()   { printf '\033[32m✓\033[0m %s\n' "$*"; }
bad()  { printf '\033[31m✗\033[0m %s\n' "$*"; }

# Sts2Path pins the compile against the SAME install that STS2_GAME_DIR installs into. Without it
# MSBuild runs its own Steam-library detection, so pointing STS2_GAME_DIR at a second install (the
# default-branch copy, say) would still compile against the first one's sts2.dll and load with a
# ReflectionTypeLoadException.
do_build()   { step "build";   cd "$REPO"; dotnet build -p:Sts2Path="$STS2_GAME_DIR"; stamp_game_build; }
do_import()  { step "import (godot)"; cd "$REPO"; "$GODOT" --headless --import --path . 2>&1 | grep -iE "error|reimport" | grep -viE "EditorSettings|TypeNameResolver" || true; }
do_publish() { step "publish"; cd "$REPO"; dotnet publish -c Debug -p:Sts2Path="$STS2_GAME_DIR"; stamp_game_build; }

# ── which game build is installed, and which one did we compile against ──────
# Steam records the branch in appmanifest_2868840.acf: BetaKey is "public-beta" on the beta branch
# and absent on the default branch. buildid changes on every update of either.
STEAM_APPS="$(dirname "$(dirname "$STS2_GAME_DIR")")"
APP_MANIFEST="$STEAM_APPS/appmanifest_2868840.acf"

game_branch() {
  [ -f "$APP_MANIFEST" ] || { echo ""; return; }
  sed -nE 's/.*"BetaKey"[[:space:]]+"([^"]*)".*/\1/p' "$APP_MANIFEST" | head -1
}
game_build_id() {
  [ -f "$APP_MANIFEST" ] || { echo ""; return; }
  sed -nE 's/.*"buildid"[[:space:]]+"([^"]*)".*/\1/p' "$APP_MANIFEST" | head -1
}

# The installed mod is a compiled binary, so it silently belongs to one game build. Record which,
# so doctor can catch the "switched Steam branch, forgot to republish" case before the game does.
BUILD_STAMP="$REPO/.tooling/built-against"
stamp_game_build() {
  mkdir -p "$(dirname "$BUILD_STAMP")"
  printf '%s\n%s\n' "$(game_branch)" "$(game_build_id)" > "$BUILD_STAMP"
}
do_verify()  {
  step "verify pck"
  [ -f "$PCK" ] && ls -lh "$PCK" || { echo "!! the pck is missing at $PCK"; exit 1; }
  # Godot keeps the pck open, so replacing it under a running game makes every later asset load
  # from it fail. The first failure throws out of NCombatUi.Activate and combat starts with no
  # background, which is easy to mistake for a mod bug.
  if pgrep -f "SlayTheSpire2" >/dev/null 2>&1; then
    bad "the game is active; it still uses the OLD pck and will throw AssetLoadException"
  fi
}

# ── release plumbing ────────────────────────────────────────────────────────
CHANGELOG="$REPO/CHANGELOG.md"
MANIFEST="$REPO/Alchemist.json"
DIST="$REPO/dist"

# ── release targets ─────────────────────────────────────────────────────────
# The branch you are on picks the Workshop item, the tag suffix, and whether the GitHub Release is
# a pre-release. workshop/targets.json holds the map and is identical on both branches, so a merge
# never conflicts over it.
TARGETS="$REPO/workshop/targets.json"

current_branch() { git -C "$REPO" rev-parse --abbrev-ref HEAD; }

# Reads one field of the current branch's target. Fails loudly for a branch with no target, which
# is how a release from a feature branch stops before it touches anything.
target_get() {  # <field>
  have_py || { bad "$no_py_msg (the release target map needs it)"; exit 1; }
  "${PY_CMD[@]}" - "$TARGETS" "$(current_branch)" "$1" <<'PYEOF'
import json, sys
path, branch, field = sys.argv[1:4]
with open(path, encoding="utf-8") as f: targets = json.load(f)
target = targets.get(branch)
if target is None:
    named = ", ".join(k for k in targets if not k.startswith("_"))
    sys.exit(f"branch '{branch}' has no release target in workshop/targets.json (it knows: {named})")
print(target.get(field, ""))
PYEOF
}

current_version() {  # the bare X.Y.Z from the manifest (it removes the v prefix)
  grep '"version"' "$MANIFEST" | sed -E 's/.*"version"[[:space:]]*:[[:space:]]*"v?([0-9]+\.[0-9]+\.[0-9]+)".*/\1/'
}

# The lines of the ## [Unreleased] section (between that heading and the next ## heading).
unreleased_body() {
  awk '/^## \[Unreleased\]/{grab=1; next} grab && /^## /{exit} grab{print}' "$CHANGELOG"
}

# The body of one version's section. A promote reads this instead of Unreleased, because the notes
# it ships came over in the merge and were rolled under their version heading on beta.
version_body() {  # <X.Y.Z>
  awk -v ver="$1" '
    $0 ~ "^## \\[" ver "\\]" {grab=1; next}
    grab && /^## \[/ {exit}
    grab {print}
  ' "$CHANGELOG"
}

# Draft the changelog entries from the Conventional Commits since the last tag. This is read-only.
do_changelog() {
  local last range subject
  last="$(git -C "$REPO" describe --tags --abbrev=0 2>/dev/null || true)"
  range="${last:+$last..HEAD}"
  step "changelog draft ${last:+since $last}${last:-(all history)}"
  emit() {  # <heading> <grep-extended-prefix>
    local out
    out="$(git -C "$REPO" log --no-merges --pretty=format:'%s' $range \
           | grep -E "^($2)(\(.+\))?!?:" \
           | sed -E "s/^($2)(\(.+\))?!?:[[:space:]]*//" | sed 's/^/- /')"
    [ -n "$out" ] && printf '\n### %s\n%s\n' "$1" "$out"
  }
  echo "Paste the correct lines under ## [Unreleased] in CHANGELOG.md. Then write them"
  echo "again in player language (see RELEASING.md). This command wrote nothing."
  emit Added   'feat'
  emit Fixed   'fix'
  emit Changed 'refactor|perf'
  emit Other   'style|docs|test|chore|build|ci'
  echo
}

do_release() {  # <patch|minor|major|X.Y.Z|promote>
  local bump="${1:-}"
  [ -n "$bump" ] || { bad "usage: scripts/dev.sh release <patch|minor|major|X.Y.Z|promote>"; exit 1; }
  have_py || { bad "$no_py_msg (release runs the lint check)"; exit 1; }

  step "release preflight"
  [ -z "$(git -C "$REPO" status --porcelain)" ] || { bad "the working tree is not clean; commit or stash your changes first"; exit 1; }

  local branch suffix item title; branch="$(current_branch)"
  suffix="$(target_get tagSuffix)" || exit 1
  item="$(target_get itemId)"; title="$(target_get title)"
  [ -n "$item" ] || { bad "workshop/targets.json has no itemId for '$branch'; create the Workshop item and fill it in first"; exit 1; }
  ok "target: $branch → \"$title\" (item $item), tags vX.Y.Z${suffix}"

  # A build compiled against the other Steam branch loads with a ReflectionTypeLoadException, and
  # nothing before the game starts would catch it. Refuse to package one.
  local want_branch; want_branch="$(target_get gameBranch)"
  local have_branch; have_branch="$(game_branch)"
  [ "$have_branch" = "$want_branch" ] || {
    bad "'$branch' releases against the game's ${want_branch:-default} branch, but the install at"
    bad "$STS2_GAME_DIR is on ${have_branch:-the default branch}. Switch the Steam branch, or point"
    bad "STS2_GAME_DIR at the right install (see BUILD.md), then run this again."; exit 1; }

  local cur new; cur="$(current_version)"
  [ -n "$cur" ] || { bad "could not read the version from $MANIFEST"; exit 1; }
  local IFS=. ; local -a p=($cur); unset IFS
  case "$bump" in
    major)   new="$((p[0]+1)).0.0" ;;
    minor)   new="${p[0]}.$((p[1]+1)).0" ;;
    patch)   new="${p[0]}.${p[1]}.$((p[2]+1))" ;;
    promote) new="$cur" ;;
    v*)      new="${bump#v}" ;;
    *)       new="$bump" ;;
  esac
  [[ "$new" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { bad "the version '$new' is not valid; the correct form is X.Y.Z"; exit 1; }
  # promote ships the version that came over in the merge, so it is the one case where the version
  # does not move. Every other form must move it forward.
  if [ "$bump" = "promote" ]; then
    git -C "$REPO" rev-parse -q --verify "refs/tags/v$new$suffix" >/dev/null &&
      { bad "v$new$suffix already exists; promote ships the merged version, so there is nothing new here"; exit 1; }
    ok "promote: keeping v$new, the version merged from beta"
  else
    [ "$new" != "$cur" ] || { bad "the new version is the same as the current version ($cur)"; exit 1; }
    [ "$(printf '%s\n%s\n' "$cur" "$new" | sort -V | tail -1)" = "$new" ] || { bad "the new version $new is not larger than the current version $cur"; exit 1; }
  fi

  # A release must have notes that a person wrote. A promote ships the section the merge brought
  # over, so it reads that version's own heading; an empty Unreleased is the correct state there.
  if [ "$bump" = "promote" ]; then
    [ -n "$(version_body "$new" | tr -d '[:space:]')" ] || { bad "## [$new] in CHANGELOG.md is empty or missing; the merge from beta should have brought its notes over"; exit 1; }
  else
    [ -n "$(unreleased_body | tr -d '[:space:]')" ] || { bad "## [Unreleased] in CHANGELOG.md is empty; add notes (scripts/dev.sh changelog makes a draft)"; exit 1; }
  fi

  do_build
  step "lint"
  "${PY_CMD[@]}" "$REPO/scripts/lint_sync.py"

  local date; date="$(date +%Y-%m-%d)"
  ok "release v$cur → v$new ($date)"

  sed -i.bak -E "s/(\"version\"[[:space:]]*:[[:space:]]*\")v?[0-9]+\.[0-9]+\.[0-9]+(\")/\1v$new\2/" "$MANIFEST" && rm -f "$MANIFEST.bak"

  # Roll the changelog: keep the Unreleased heading empty and add a heading for the new version.
  # A promote already has its heading, because the merge brought beta's rolled changelog with it.
  if ! grep -q "^## \[$new\]" "$CHANGELOG"; then
    awk -v ver="$new" -v date="$date" '
      /^## \[Unreleased\]/ { print; print ""; print "## [" ver "] - " date; next }
      { print }
    ' "$CHANGELOG" > "$CHANGELOG.tmp" && mv "$CHANGELOG.tmp" "$CHANGELOG"
  fi

  do_publish

  # One top-level Alchemist/ folder with the three runtime files the game loads. No pdb, which is
  # for debug only, and the mod_image is already in the pck.
  local tag="v$new$suffix"
  step "package dist/Alchemist-$tag.zip"
  local stage="$DIST/stage" src="$GAME_MODS/Alchemist"
  rm -rf "$stage"; mkdir -p "$stage/Alchemist"
  local f; for f in Alchemist.dll Alchemist.json Alchemist.pck; do
    [ -f "$src/$f" ] || { bad "$src/$f must exist after the publish; the release stops here"; exit 1; }
    cp -f "$src/$f" "$stage/Alchemist/"
  done
  local zipfile="$DIST/Alchemist-$tag.zip"
  rm -f "$zipfile"
  (cd "$stage" && zip -r -q "$zipfile" Alchemist)
  rm -rf "$stage"
  ls -lh "$zipfile"

  # Notes for the GitHub Release body or the Workshop comment. The version heading is dropped,
  # because both paste targets supply their own title. The changelog wraps its bullets to keep
  # the markdown source readable, and both targets wrap text themselves, so the second stage
  # unwraps: a bullet at any depth starts a line, and an indented non-bullet joins the one above.
  local notes="$DIST/RELEASE_NOTES-$tag.txt"
  awk -v ver="$new" '
    $0 ~ "^## \\[" ver "\\]" {grab=1; next}
    grab && /^## \[/ {exit}
    grab && !started && /^[[:space:]]*$/ {next}
    grab {started=1; print}
  ' "$CHANGELOG" | awk '
    function flush() { if (line != "") { print line; line = "" } }
    /^[[:space:]]*-[[:space:]]/  { flush(); line = $0; next }
    /^[[:space:]]*$/             { flush(); print; next }
    /^[[:space:]]/ && line != "" { sub(/^[[:space:]]+/, ""); line = line " " $0; next }
    { flush(); print }
    END { flush() }
  ' > "$notes"

  # Refresh the Steam Workshop workspace: the same three runtime files go into content/, and
  # workshop.json gets the new version and the release notes as its changeNote, so a
  # `ModUploader upload -w workshop` after the release pushes exactly what was packaged.
  # The title and the item id come from targets.json rather than the file, so the same committed
  # workshop.json serves both branches and a merge never has to resolve them.
  if [ -d "$REPO/workshop" ]; then
    step "workshop workspace"
    for f in Alchemist.dll Alchemist.json Alchemist.pck; do
      cp -f "$src/$f" "$REPO/workshop/content/"
    done
    printf '%s\n' "$item" > "$REPO/workshop/mod_id.txt"
    "${PY_CMD[@]}" - "$REPO/workshop/workshop.json" "v$new" "$notes" "$title" <<'PYEOF'
import json, re, sys
path, ver, notes_file, title = sys.argv[1:5]
with open(path, encoding="utf-8") as f: cfg = json.load(f)
with open(notes_file, encoding="utf-8") as f: notes = f.read().strip()
# Steam renders BBCode, not markdown, so a "### Changed" heading would ship as literal hashes.
# Drop the marks and keep the words. The changelog and the GitHub release body keep their headings.
notes = re.sub(r"(?m)^#+[ \t]*", "", notes)
cfg["title"] = title
cfg["changeNote"] = f"{ver}\n\n{notes}" if notes else ver
with open(path, "w", encoding="utf-8") as f:
    json.dump(cfg, f, indent=2, ensure_ascii=False); f.write("\n")
PYEOF
    ok "workshop/ is set for \"$title\" (item $item) at v$new"
  fi

  echo
  ok "$tag is ready: the files are updated, the artifact and the notes are in dist/"
  echo "Examine the diff, then publish it:"
  echo
  echo "    scripts/dev.sh publish-release"
  echo
  echo "That command commits, tags $tag, pushes $branch, and puts the release on GitHub with"
  echo "$zipfile attached and $notes as the body."
  echo
  echo "Steam is a separate step. Copy this line as is: --id names the item, so it cannot"
  echo "go to the other branch's item no matter what mod_id.txt holds."
  echo
  echo "    cd ~/code/sts2-mod-uploader && DOTNET_ROLL_FORWARD=LatestMajor \\"
  echo "      dotnet run -c Release --no-build -- upload --id $item -w $REPO/workshop"
}

# ── GitHub release automation ───────────────────────────────────────────────
# gh makes every network call. It reads the repository from the origin remote, so this script
# holds no URL and no token.
require_gh() {
  command -v gh >/dev/null 2>&1 || { bad "gh (the GitHub CLI) is not installed; run: brew install gh"; exit 1; }
  gh auth status >/dev/null 2>&1 || { bad "gh has no login; run: gh auth login"; exit 1; }
}

# Commit the release edit, tag it, push it, and create or update the GitHub Release.
# The tag and the push need --force only when the history moved under a tag that is already
# public. That case is a rewrite, so the command refuses to guess and asks for the flag.
do_publish_release() {  # [--force] [--draft] [vX.Y.Z]
  local force=0 draft=0 ver=""
  while [ $# -gt 0 ]; do
    case "$1" in
      --force) force=1 ;;
      --draft) draft=1 ;;
      v[0-9]*|[0-9]*) ver="${1#v}" ;;
      *) bad "unknown option '$1'; usage: scripts/dev.sh publish-release [--force] [--draft] [vX.Y.Z]"; exit 1 ;;
    esac
    shift
  done
  require_gh

  local branch suffix; branch="$(current_branch)"
  suffix="$(target_get tagSuffix)" || exit 1
  [ -n "$ver" ] || ver="$(current_version)"
  local tag="v$ver$suffix"

  # The release edit that `release` leaves behind is exactly these files (the workshop ones only
  # change when the workshop workspace exists). Commit it here so that the whole flow is two
  # commands. Any other pending change means the tree is not ready.
  if [ -n "$(git -C "$REPO" status --porcelain)" ]; then
    local extra; extra="$(git -C "$REPO" status --porcelain | awk '{print $2}' \
      | grep -v -E '^(Alchemist\.json|CHANGELOG\.md|workshop/workshop\.json|workshop/mod_id\.txt)$' || true)"
    [ -z "$extra" ] || {
      bad "the working tree has changes other than the release edit: $(echo "$extra" | tr '\n' ' ')"; exit 1; }
    step "commit release: $tag"
    git -C "$REPO" add Alchemist.json CHANGELOG.md
    [ -f "$REPO/workshop/workshop.json" ] && git -C "$REPO" add workshop/workshop.json
    [ -f "$REPO/workshop/mod_id.txt" ] && git -C "$REPO" add workshop/mod_id.txt
    git -C "$REPO" commit -q -m "release: $tag"
    ok "committed"
  fi

  local head_subject; head_subject="$(git -C "$REPO" log -1 --format=%s)"
  [ "$head_subject" = "release: $tag" ] || bad "note: the tip commit is '$head_subject', not 'release: $tag'"

  local zip="$DIST/Alchemist-$tag.zip" notes="$DIST/RELEASE_NOTES-$tag.txt"
  [ -f "$zip" ]   || { bad "$zip is missing; run: scripts/dev.sh release $ver"; exit 1; }
  [ -f "$notes" ] || { bad "$notes is missing; run: scripts/dev.sh release $ver"; exit 1; }

  # Put the tag on the tip commit. A tag that already points somewhere else only moves with --force.
  step "tag $tag"
  local head; head="$(git -C "$REPO" rev-parse HEAD)"
  local tagged=""; tagged="$(git -C "$REPO" rev-parse -q --verify "refs/tags/$tag^{commit}" 2>/dev/null || true)"
  if [ -z "$tagged" ]; then
    git -C "$REPO" tag -a "$tag" -m "$tag"; ok "created $tag"
  elif [ "$tagged" = "$head" ]; then
    ok "$tag already points at the tip commit"
  else
    [ "$force" -eq 1 ] || { bad "$tag points at $tagged, not the tip commit $head; pass --force to move it"; exit 1; }
    git -C "$REPO" tag -f -a "$tag" -m "$tag" >/dev/null; ok "moved $tag to $head"
  fi

  # --force-with-lease is the safe force: it stops if the remote holds a commit that this clone
  # has never seen. A plain --force would drop that work without a word.
  step "push $branch and $tag"
  if [ "$force" -eq 1 ]; then
    git -C "$REPO" push --force-with-lease origin "$branch"
    git -C "$REPO" push --force origin "refs/tags/$tag"
  else
    git -C "$REPO" push origin "$branch"
    git -C "$REPO" push origin "refs/tags/$tag"
  fi
  ok "pushed"

  # A beta release is a pre-release, so GitHub keeps its "Latest release" pointing at the newest
  # main release. Someone who lands on the repo without reading anything gets the public build.
  local pre; pre="$(target_get prerelease)"
  local -a flags=(--title "$tag" --notes-file "$notes")
  [ "$draft" -eq 1 ] && flags+=(--draft)
  [ "$pre" = "True" ] && flags+=(--prerelease)

  if gh release view "$tag" --repo "$(gh repo view --json nameWithOwner -q .nameWithOwner)" >/dev/null 2>&1; then
    step "update the GitHub Release $tag"
    # The explicit false clears the flag on a release that an earlier run marked pre-release.
    [ "$pre" = "True" ] || flags+=(--prerelease=false)
    gh release edit "$tag" "${flags[@]}" >/dev/null
    gh release upload "$tag" "$zip" --clobber
    ok "updated"
  else
    step "create the GitHub Release $tag"
    gh release create "$tag" "$zip" "${flags[@]}" >/dev/null
    ok "created"
  fi

  echo
  ok "$tag is live: $(gh release view "$tag" --json url -q .url)"
}

# ── promoting beta into main ────────────────────────────────────────────────
# A merge, never a rebase. Rebasing would move commits that public tags already point at, and both
# the tags and the release artifacts attached to them would then describe a history that is gone.
do_sync_main() {
  step "sync main ← beta"
  [ -z "$(git -C "$REPO" status --porcelain)" ] || { bad "the working tree is not clean; commit or stash your changes first"; exit 1; }
  local branch; branch="$(current_branch)"
  [ "$branch" = "main" ] || { bad "you are on '$branch'; check out main first (git switch main)"; exit 1; }
  git -C "$REPO" rev-parse -q --verify refs/heads/beta >/dev/null || { bad "there is no beta branch in this clone"; exit 1; }

  local behind; behind="$(git -C "$REPO" rev-list --count main..beta)"
  [ "$behind" -gt 0 ] || { ok "main already has everything on beta; nothing to promote"; return 0; }
  echo "  $behind commit(s) to bring over:"
  git -C "$REPO" log --oneline --no-decorate main..beta | sed 's/^/    /'

  echo
  git -C "$REPO" merge --no-ff --no-edit beta || {
    bad "the merge stopped with conflicts. Resolve them, 'git add' the files, then 'git commit'."
    bad "Take beta's side for content, and keep main's side for anything main fixed on its own."
    exit 1; }
  ok "merged; main is now at v$(current_version)"
  echo
  echo "Next: verify it on the game's default branch (see BUILD.md), then"
  echo
  echo "    scripts/dev.sh release promote"
  echo "    scripts/dev.sh publish-release"
}

do_doctor() {
  step "doctor"
  local fail=0
  if command -v dotnet >/dev/null;   then ok "dotnet $(dotnet --version 2>/dev/null)"; else bad "dotnet not found; install the .NET 9 SDK (https://dotnet.microsoft.com)"; fail=1; fi
  if have_py;                        then ok "python $("${PY_CMD[@]}" --version 2>&1 | cut -d' ' -f2) (${PY_CMD[*]})"; else bad "no Python 3.10 or later; scripts/dev.sh lint needs it; install uv (https://astral.sh/uv) to get one, or install Python directly"; fail=1; fi
  if [ -x "$GODOT" ];                then ok "Godot at $GODOT"; else bad "Godot not found at $GODOT; install Godot 4.5.1 (.NET), or set GODOT=/path/to/Godot (see BUILD.md)"; fail=1; fi
  if [ -d "$STS2_GAME_DIR" ];        then ok "game at $STS2_GAME_DIR"; else bad "game not found at $STS2_GAME_DIR; install it through Steam, or set STS2_GAME_DIR"; fail=1; fi
  if [ -d "$GAME_MODS/Alchemist" ];  then ok "Alchemist mod installed"; else bad "Alchemist mod not installed; run scripts/dev.sh publish"; fail=1; fi

  # The branch you are on, the Steam branch you are running, and the game build the installed mod
  # was compiled against all have to agree. A mismatch shows up only as a
  # ReflectionTypeLoadException when the game loads, which names none of this.
  local branch; branch="$(current_branch)"
  local have_branch; have_branch="$(game_branch)"
  ok "git branch $branch, game on ${have_branch:-the default branch} (build $(game_build_id))"
  if have_py && [ -f "$TARGETS" ]; then
    local want_branch; want_branch="$(target_get gameBranch 2>/dev/null || echo SKIP)"
    if [ "$want_branch" != "SKIP" ] && [ "$want_branch" != "$have_branch" ]; then
      bad "branch '$branch' expects the game's ${want_branch:-default} branch; switch the Steam branch or check out the other git branch"
      fail=1
    fi
  fi
  if [ -f "$BUILD_STAMP" ]; then
    local built; built="$(sed -n 2p "$BUILD_STAMP")"
    if [ "$built" != "$(game_build_id)" ]; then
      bad "the installed mod was built against game build $built, the game is now $(game_build_id); run scripts/dev.sh publish"
      fail=1
    else ok "the installed mod matches the current game build"; fi
  else bad "note: no build stamp yet; run scripts/dev.sh publish to record which game build the mod was compiled against"; fi
  # gh is necessary only for publish-release, so a missing gh is a note, not a failure.
  if ! command -v gh >/dev/null 2>&1; then bad "note: gh is not installed; scripts/dev.sh publish-release needs it (brew install gh)"
  elif gh auth status >/dev/null 2>&1; then ok "gh is logged in ($(gh api user -q .login 2>/dev/null))"
  else bad "note: gh has no login; scripts/dev.sh publish-release needs one (gh auth login)"; fi
  [ "$fail" -eq 0 ] && { echo; ok "the environment is correct"; } || { echo; bad "correct the items above, then run scripts/dev.sh doctor again"; }
  return "$fail"
}

case "${1:-help}" in
  publish)       do_build; do_import; do_publish; do_verify ;;
  publish-fast)  do_build; do_publish; do_verify ;;
  import)        do_import ;;
  lint)          have_py || { bad "$no_py_msg"; exit 1; }
                 "${PY_CMD[@]}" "$REPO/scripts/lint_sync.py" ;;
  changelog)     do_changelog ;;
  release)       shift; do_release "$@" ;;
  publish-release) shift; do_publish_release "$@" ;;
  sync-main)     do_sync_main ;;
  doctor)        do_doctor ;;
  env)
    echo "REPO          = $REPO"
    echo "STS2_GAME_DIR = $STS2_GAME_DIR"
    echo "GAME_MODS     = $GAME_MODS"
    echo "GODOT         = $GODOT"
    echo "PCK           = $PCK"
    echo "GIT BRANCH    = $(current_branch)"
    echo "GAME BRANCH   = $(game_branch) (build $(game_build_id))"
    if have_py && [ -f "$TARGETS" ]; then
      echo "WORKSHOP ITEM = $(target_get itemId 2>/dev/null || echo 'none for this branch')"
      echo "TAG FORMAT    = v<version>$(target_get tagSuffix 2>/dev/null || echo '')"
    fi
    ;;
  *)
    grep -E '^#( |$)' "${BASH_SOURCE[0]}" | sed -E 's/^# ?//'
    ;;
esac
