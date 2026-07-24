#!/usr/bin/env bash
# Dev helper for the Alchemist mod. One command does each repeated loop.
#
#   scripts/dev.sh publish        build → godot import → publish → verify pck   (the safe default)
#   scripts/dev.sh publish-fast   build → publish → verify pck                  (code only, no import)
#   scripts/dev.sh import         godot --headless --import only
#   scripts/dev.sh lint           static check of the three-way rule (offline, no game)
#   scripts/dev.sh changelog      draft CHANGELOG entries from the commits since the last tag
#                                 (it prints only, it writes nothing)
#   scripts/dev.sh release <patch|minor|major|X.Y.Z>
#                                 increase the version, roll the CHANGELOG, build, and package
#                                 dist/Alchemist-vX.Y.Z.zip (see RELEASING.md)
#   scripts/dev.sh publish-release [--force] [--draft] [vX.Y.Z]
#                                 commit the release edit, tag it, push it, and create or update
#                                 the GitHub Release with the zip and the notes. --force moves a
#                                 tag that is already public (a history rewrite)
#   scripts/dev.sh doctor         check every prerequisite and print ✓/✗ with the fixes
#   scripts/dev.sh env            print the resolved paths and exit
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

do_build()   { step "build";   cd "$REPO"; dotnet build; }
do_import()  { step "import (godot)"; cd "$REPO"; "$GODOT" --headless --import --path . 2>&1 | grep -iE "error|reimport" | grep -viE "EditorSettings|TypeNameResolver" || true; }
do_publish() { step "publish"; cd "$REPO"; dotnet publish -c Debug; }
do_verify()  {
  step "verify pck"
  [ -f "$PCK" ] && ls -lh "$PCK" || { echo "!! the pck is missing at $PCK"; exit 1; }
  # Godot keeps the pck open. If you replace the pck while the game is active, every later
  # asset load from it fails. The first failure (the custom energy counter) throws out of
  # NCombatUi.Activate, and combat then starts with no background. The result is easy to
  # confuse with a mod bug, but the mod is not the cause.
  if pgrep -f "SlayTheSpire2" >/dev/null 2>&1; then
    bad "the game is active; it still uses the OLD pck and will throw AssetLoadException"
  fi
}

# ── release plumbing ────────────────────────────────────────────────────────
CHANGELOG="$REPO/CHANGELOG.md"
MANIFEST="$REPO/Alchemist.json"
DIST="$REPO/dist"

current_version() {  # the bare X.Y.Z from the manifest (it removes the v prefix)
  grep '"version"' "$MANIFEST" | sed -E 's/.*"version"[[:space:]]*:[[:space:]]*"v?([0-9]+\.[0-9]+\.[0-9]+)".*/\1/'
}

# The lines of the ## [Unreleased] section (between that heading and the next ## heading).
unreleased_body() {
  awk '/^## \[Unreleased\]/{grab=1; next} grab && /^## /{exit} grab{print}' "$CHANGELOG"
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

do_release() {  # <patch|minor|major|X.Y.Z>
  local bump="${1:-}"
  [ -n "$bump" ] || { bad "usage: scripts/dev.sh release <patch|minor|major|X.Y.Z>"; exit 1; }
  have_py || { bad "$no_py_msg (release runs the lint check)"; exit 1; }

  step "release preflight"
  [ -z "$(git -C "$REPO" status --porcelain)" ] || { bad "the working tree is not clean; commit or stash your changes first"; exit 1; }
  local branch; branch="$(git -C "$REPO" rev-parse --abbrev-ref HEAD)"
  [ "$branch" = "main" ] || { bad "you are on branch '$branch'; do the release from main"; exit 1; }

  # Calculate the new version.
  local cur new; cur="$(current_version)"
  [ -n "$cur" ] || { bad "could not read the version from $MANIFEST"; exit 1; }
  local IFS=. ; local -a p=($cur); unset IFS
  case "$bump" in
    major) new="$((p[0]+1)).0.0" ;;
    minor) new="${p[0]}.$((p[1]+1)).0" ;;
    patch) new="${p[0]}.${p[1]}.$((p[2]+1))" ;;
    v*)    new="${bump#v}" ;;
    *)     new="$bump" ;;
  esac
  [[ "$new" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { bad "the version '$new' is not valid; the correct form is X.Y.Z"; exit 1; }
  [ "$new" != "$cur" ] || { bad "the new version is the same as the current version ($cur)"; exit 1; }
  [ "$(printf '%s\n%s\n' "$cur" "$new" | sort -V | tail -1)" = "$new" ] || { bad "the new version $new is not larger than the current version $cur"; exit 1; }

  # A release must have notes that a person wrote.
  [ -n "$(unreleased_body | tr -d '[:space:]')" ] || { bad "## [Unreleased] in CHANGELOG.md is empty; add notes (scripts/dev.sh changelog makes a draft)"; exit 1; }

  do_build
  step "lint"
  "${PY_CMD[@]}" "$REPO/scripts/lint_sync.py"

  local date; date="$(date +%Y-%m-%d)"
  ok "release v$cur → v$new ($date)"

  # Increase the manifest version (keep the v prefix).
  sed -i.bak -E "s/(\"version\"[[:space:]]*:[[:space:]]*\")v?[0-9]+\.[0-9]+\.[0-9]+(\")/\1v$new\2/" "$MANIFEST" && rm -f "$MANIFEST.bak"

  # Roll the changelog: keep the Unreleased heading empty and add a heading for the new version.
  awk -v ver="$new" -v date="$date" '
    /^## \[Unreleased\]/ { print; print ""; print "## [" ver "] - " date; next }
    { print }
  ' "$CHANGELOG" > "$CHANGELOG.tmp" && mv "$CHANGELOG.tmp" "$CHANGELOG"

  do_publish

  # Package the zip that a user installs: one top-level Alchemist/ folder with the three
  # runtime files that the game loads. It has no pdb, which is for debug only. The
  # mod_image is in the pck.
  step "package dist/Alchemist-v$new.zip"
  local stage="$DIST/stage" src="$GAME_MODS/Alchemist"
  rm -rf "$stage"; mkdir -p "$stage/Alchemist"
  local f; for f in Alchemist.dll Alchemist.json Alchemist.pck; do
    [ -f "$src/$f" ] || { bad "$src/$f must exist after the publish; the release stops here"; exit 1; }
    cp -f "$src/$f" "$stage/Alchemist/"
  done
  local zipfile="$DIST/Alchemist-v$new.zip"
  rm -f "$zipfile"
  (cd "$stage" && zip -r -q "$zipfile" Alchemist)
  rm -rf "$stage"
  ls -lh "$zipfile"

  # Get the notes of this version for the GitHub Release body or the Workshop comment.
  # The changelog wraps its bullets to keep the markdown source readable. Both paste
  # targets wrap text themselves, so the second stage puts each bullet back on one line.
  # A bullet at any depth starts a new line; an indented line that is not a bullet is a
  # wrap of the bullet above it and joins back onto it.
  local notes="$DIST/RELEASE_NOTES-v$new.txt"
  awk -v ver="$new" '
    $0 ~ "^## \\[" ver "\\]" {grab=1; print; next}
    grab && /^## \[/ {exit}
    grab {print}
  ' "$CHANGELOG" | awk '
    function flush() { if (line != "") { print line; line = "" } }
    /^[[:space:]]*-[[:space:]]/  { flush(); line = $0; next }
    /^[[:space:]]*$/             { flush(); print; next }
    /^[[:space:]]/ && line != "" { sub(/^[[:space:]]+/, ""); line = line " " $0; next }
    { flush(); print }
    END { flush() }
  ' > "$notes"

  echo
  ok "v$new is ready: the files are updated, the artifact and the notes are in dist/"
  echo "Examine the diff, then publish it:"
  echo
  echo "    scripts/dev.sh publish-release"
  echo
  echo "That command commits, tags, pushes, and puts the release on GitHub with"
  echo "$zipfile attached and $notes as the body."
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

  local branch; branch="$(git -C "$REPO" rev-parse --abbrev-ref HEAD)"
  [ "$branch" = "main" ] || { bad "you are on branch '$branch'; publish the release from main"; exit 1; }
  [ -n "$ver" ] || ver="$(current_version)"
  local tag="v$ver"

  # The release edit that `release` leaves behind is exactly these two files. Commit it here so
  # that the whole flow is two commands. Any other pending change means the tree is not ready.
  if [ -n "$(git -C "$REPO" status --porcelain)" ]; then
    local dirty; dirty="$(git -C "$REPO" status --porcelain | awk '{print $2}' | sort | tr '\n' ' ')"
    [ "$dirty" = "Alchemist.json CHANGELOG.md " ] || {
      bad "the working tree has changes other than the release edit: $dirty"; exit 1; }
    step "commit release: $tag"
    git -C "$REPO" add Alchemist.json CHANGELOG.md
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
  step "push main and $tag"
  if [ "$force" -eq 1 ]; then
    git -C "$REPO" push --force-with-lease origin main
    git -C "$REPO" push --force origin "refs/tags/$tag"
  else
    git -C "$REPO" push origin main
    git -C "$REPO" push origin "refs/tags/$tag"
  fi
  ok "pushed"

  # Before 1.0 every release is a pre-release: 1.0.0 is the first Steam Workshop version.
  local -a flags=(--title "$tag" --notes-file "$notes")
  [ "${ver%%.*}" = "0" ] && flags+=(--prerelease)
  [ "$draft" -eq 1 ] && flags+=(--draft)

  if gh release view "$tag" --repo "$(gh repo view --json nameWithOwner -q .nameWithOwner)" >/dev/null 2>&1; then
    step "update the GitHub Release $tag"
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

do_doctor() {
  step "doctor"
  local fail=0
  if command -v dotnet >/dev/null;   then ok "dotnet $(dotnet --version 2>/dev/null)"; else bad "dotnet not found; install the .NET 9 SDK (https://dotnet.microsoft.com)"; fail=1; fi
  if have_py;                        then ok "python $("${PY_CMD[@]}" --version 2>&1 | cut -d' ' -f2) (${PY_CMD[*]})"; else bad "no Python 3.10 or later; scripts/dev.sh lint needs it; install uv (https://astral.sh/uv) to get one, or install Python directly"; fail=1; fi
  if [ -x "$GODOT" ];                then ok "Godot at $GODOT"; else bad "Godot not found at $GODOT; install Godot 4.5.1 (.NET), or set GODOT=/path/to/Godot (see BUILD.md)"; fail=1; fi
  if [ -d "$STS2_GAME_DIR" ];        then ok "game at $STS2_GAME_DIR"; else bad "game not found at $STS2_GAME_DIR; install it through Steam, or set STS2_GAME_DIR"; fail=1; fi
  if [ -d "$GAME_MODS/Alchemist" ];  then ok "Alchemist mod installed"; else bad "Alchemist mod not installed; run scripts/dev.sh publish"; fail=1; fi
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
  doctor)        do_doctor ;;
  env)
    echo "REPO          = $REPO"
    echo "STS2_GAME_DIR = $STS2_GAME_DIR"
    echo "GAME_MODS     = $GAME_MODS"
    echo "GODOT         = $GODOT"
    echo "PCK           = $PCK"
    ;;
  *)
    grep -E '^#( |$)' "${BASH_SOURCE[0]}" | sed -E 's/^# ?//'
    ;;
esac
