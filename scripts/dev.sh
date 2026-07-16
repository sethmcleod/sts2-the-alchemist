#!/usr/bin/env bash
# Dev helper for the Alchemist mod. One command for each tedious loop.
#
#   scripts/dev.sh setup          first-time setup: clone tooling, check deps, install bridge mods
#   scripts/dev.sh publish        build → godot import → publish → verify pck   (safe default)
#   scripts/dev.sh publish-fast   build → publish → verify pck                  (code-only, skips import)
#   scripts/dev.sh import         godot --headless --import only
#   scripts/dev.sh bridge         build + install the MCPTest + GodotExplorer bridge mods into the game
#   scripts/dev.sh test [args]    run the regression suite (starts the game if needed;
#                                 args: --group NAME, --fresh, name filters; see scripts/tests/README.md)
#   scripts/dev.sh game-start     launch the game via Steam and wait for the bridge
#   scripts/dev.sh game-stop      quit the game (graceful, then force)
#   scripts/dev.sh game-restart   stop + start (loads freshly-installed bridge/mod builds)
#   scripts/dev.sh lint           static three-way-rule check (offline, no game)
#   scripts/dev.sh changelog      draft CHANGELOG entries from commits since the last tag (prints, writes nothing)
#   scripts/dev.sh release <patch|minor|major|X.Y.Z>
#                                 bump version, roll CHANGELOG, build, and package dist/Alchemist-vX.Y.Z.zip;
#                                 prints the git commit/tag/push block for you to run (see RELEASING.md)
#   scripts/dev.sh doctor         check every prerequisite and print ✓/✗ with fixes
#   scripts/dev.sh env            print the resolved paths and exit
#
# Why this exists: every publish needs PATH + DOTNET_ROLL_FORWARD set, the game dir exported for the
# bridge build, and 3–4 chained commands. This wraps all of that so the inner loop is a single word.
#
# Every path below is overridable via environment variables (see env output); the defaults
# auto-detect the platform so a fresh clone works without editing this file.
set -euo pipefail

# ── environment every command depends on ────────────────────────────────────
# dotnet often lives outside the default login PATH; RollForward=Major lets the net9 analyzers run on
# newer runtimes; STS2_GAME_DIR is what the bridge csprojs read to find sts2.dll.
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet/tools"
export DOTNET_ROLL_FORWARD=Major

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Game install dir: env override, else the platform's default Steam library.
if [ -z "${STS2_GAME_DIR:-}" ]; then
  case "$(uname -s)" in
    Darwin) STS2_GAME_DIR="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2" ;;
    Linux)  STS2_GAME_DIR="$HOME/.steam/steam/steamapps/common/Slay the Spire 2" ;;
    *)      STS2_GAME_DIR="C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2" ;;
  esac
fi
export STS2_GAME_DIR

# Mods dir: inside the .app bundle on macOS, next to the executable elsewhere.
if [ -d "$STS2_GAME_DIR/SlayTheSpire2.app" ]; then
  GAME_MODS="$STS2_GAME_DIR/SlayTheSpire2.app/Contents/MacOS/mods"
else
  GAME_MODS="$STS2_GAME_DIR/mods"
fi

# sts2-modding-mcp checkout (bridge mods + test engine): env override, then the
# repo-local clone made by `setup`, then a shared ~/code checkout.
if [ -z "${STS2_MCP_DIR:-}" ]; then
  if   [ -d "$REPO/.tooling/sts2-modding-mcp" ]; then STS2_MCP_DIR="$REPO/.tooling/sts2-modding-mcp"
  elif [ -d "$HOME/code/sts2-modding-mcp" ];     then STS2_MCP_DIR="$HOME/code/sts2-modding-mcp"
  else STS2_MCP_DIR="$REPO/.tooling/sts2-modding-mcp"   # where `setup` will put it
  fi
fi

GODOT="${GODOT:-/Applications/MegaDot.app/Contents/MacOS/Godot}"
PCK="$GAME_MODS/Alchemist/Alchemist.pck"

# The test engine (sts2mcp) needs Python >= 3.10. Prefer a system python if one's already on PATH;
# otherwise fall back to uv, which provisions a suitable Python for you (recommended, see BUILD.md).
# PY_CMD is the interpreter invocation as an array, since the uv form is multi-word.
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
no_py_msg="no Python >= 3.10 found; install uv (https://astral.sh/uv) and it'll provision one, or install Python directly"

step() { printf '\n\033[1;36m▶ %s\033[0m\n' "$*"; }
ok()   { printf '\033[32m✓\033[0m %s\n' "$*"; }
bad()  { printf '\033[31m✗\033[0m %s\n' "$*"; }

port_open() { (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null && exec 3>&- && return 0 || return 1; }

do_build()   { step "build";   cd "$REPO"; dotnet build; }
do_import()  { step "import (godot)"; cd "$REPO"; "$GODOT" --headless --import --path . 2>&1 | grep -iE "error|reimport" | grep -viE "EditorSettings|TypeNameResolver" || true; }
do_publish() { step "publish"; cd "$REPO"; dotnet publish -c Debug; }
do_verify()  {
  step "verify pck"
  [ -f "$PCK" ] && ls -lh "$PCK" || { echo "!! pck missing at $PCK"; exit 1; }
}

# Build one bridge mod project from the tooling checkout and copy its runtime files into
# the game mods dir. NOT 0Harmony.dll, since the game ships its own and a duplicate conflicts.
install_bridge_mod() {  # <project-subdir> <csproj> <dll-base> <mods-subdir>
  local src="$STS2_MCP_DIR/$1" csproj="$2" base="$3" dest="$GAME_MODS/$4"
  step "build $base"
  [ -d "$src" ] || { bad "missing $src; run scripts/dev.sh setup first"; exit 1; }
  (cd "$src"; dotnet build "$csproj")
  step "install $base → $dest"
  mkdir -p "$dest"
  for f in "$base.dll" "$base.deps.json" "$base.runtimeconfig.json" "$base.pdb" mod_manifest.json; do
    cp -f "$src/bin/Debug/net9.0/$f" "$dest/" 2>/dev/null || cp -f "$src/$f" "$dest/"
  done
  ls "$dest"
}

do_bridge() {
  install_bridge_mod test_mod     MCPTest.csproj       MCPTest       mcptest
  install_bridge_mod explorer_mod GodotExplorer.csproj GodotExplorer godotexplorer
  echo
  echo "Now launch STS2 via Steam. MCPTest listens on TCP 21337, GodotExplorer on 27020."
}

do_test() {
  step "regression suite"
  have_py || { bad "$no_py_msg"; exit 1; }
  PYTHONPATH="$STS2_MCP_DIR" STS2_MCP_DIR="$STS2_MCP_DIR" "${PY_CMD[@]}" "$REPO/scripts/tests/run_suite.py" "$@"
}

do_game() {  # start|stop|restart
  have_py || { bad "$no_py_msg"; exit 1; }
  PYTHONPATH="$STS2_MCP_DIR" STS2_MCP_DIR="$STS2_MCP_DIR" "${PY_CMD[@]}" "$REPO/scripts/tests/run_suite.py" --game "$1"
}

# ── release plumbing ────────────────────────────────────────────────────────
CHANGELOG="$REPO/CHANGELOG.md"
MANIFEST="$REPO/Alchemist.json"
DIST="$REPO/dist"

current_version() {  # bare X.Y.Z from the manifest (strips the v prefix)
  grep '"version"' "$MANIFEST" | sed -E 's/.*"version"[[:space:]]*:[[:space:]]*"v?([0-9]+\.[0-9]+\.[0-9]+)".*/\1/'
}

# Lines of the ## [Unreleased] section (between that heading and the next ## heading).
unreleased_body() {
  awk '/^## \[Unreleased\]/{grab=1; next} grab && /^## /{exit} grab{print}' "$CHANGELOG"
}

# Draft changelog entries from Conventional Commits since the last tag. Read-only.
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
  echo "Paste the relevant lines under ## [Unreleased] in CHANGELOG.md, then reword"
  echo "them into player-facing language (see RELEASING.md). Nothing was written."
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
  [ -z "$(git -C "$REPO" status --porcelain)" ] || { bad "working tree not clean; commit or stash first"; exit 1; }
  local branch; branch="$(git -C "$REPO" rev-parse --abbrev-ref HEAD)"
  [ "$branch" = "main" ] || { bad "on branch '$branch'; release from main"; exit 1; }

  # Compute the new version.
  local cur new; cur="$(current_version)"
  [ -n "$cur" ] || { bad "could not read version from $MANIFEST"; exit 1; }
  local IFS=. ; local -a p=($cur); unset IFS
  case "$bump" in
    major) new="$((p[0]+1)).0.0" ;;
    minor) new="${p[0]}.$((p[1]+1)).0" ;;
    patch) new="${p[0]}.${p[1]}.$((p[2]+1))" ;;
    v*)    new="${bump#v}" ;;
    *)     new="$bump" ;;
  esac
  [[ "$new" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { bad "invalid version '$new'; expected X.Y.Z"; exit 1; }
  [ "$new" != "$cur" ] || { bad "new version equals current ($cur)"; exit 1; }
  [ "$(printf '%s\n%s\n' "$cur" "$new" | sort -V | tail -1)" = "$new" ] || { bad "new version $new is not greater than current $cur"; exit 1; }

  # A release must have curated notes.
  [ -n "$(unreleased_body | tr -d '[:space:]')" ] || { bad "## [Unreleased] in CHANGELOG.md is empty; add notes (scripts/dev.sh changelog seeds a draft)"; exit 1; }

  do_build
  step "lint"
  "${PY_CMD[@]}" "$REPO/scripts/lint_sync.py"

  local date; date="$(date +%Y-%m-%d)"
  ok "releasing v$cur → v$new ($date)"

  # Bump the manifest version (keep the v prefix).
  sed -i.bak -E "s/(\"version\"[[:space:]]*:[[:space:]]*\")v?[0-9]+\.[0-9]+\.[0-9]+(\")/\1v$new\2/" "$MANIFEST" && rm -f "$MANIFEST.bak"

  # Roll the changelog: freshen Unreleased and stamp the released section.
  awk -v ver="$new" -v date="$date" '
    /^## \[Unreleased\]/ { print; print ""; print "## [" ver "] - " date; next }
    { print }
  ' "$CHANGELOG" > "$CHANGELOG.tmp" && mv "$CHANGELOG.tmp" "$CHANGELOG"

  do_publish

  # Package the drop-in zip: a single top-level Alchemist/ folder with the three
  # runtime files the game loads (no pdb, which is debug-only; mod_image is inside the pck).
  step "package dist/Alchemist-v$new.zip"
  local stage="$DIST/stage" src="$GAME_MODS/Alchemist"
  rm -rf "$stage"; mkdir -p "$stage/Alchemist"
  local f; for f in Alchemist.dll Alchemist.json Alchemist.pck; do
    [ -f "$src/$f" ] || { bad "expected $src/$f after publish; aborting"; exit 1; }
    cp -f "$src/$f" "$stage/Alchemist/"
  done
  local zipfile="$DIST/Alchemist-v$new.zip"
  rm -f "$zipfile"
  (cd "$stage" && zip -r -q "$zipfile" Alchemist)
  rm -rf "$stage"
  ls -lh "$zipfile"

  # Extract this version's notes for the GitHub Release body / Workshop comment.
  local notes="$DIST/RELEASE_NOTES-v$new.txt"
  awk -v ver="$new" '
    $0 ~ "^## \\[" ver "\\]" {grab=1; print; next}
    grab && /^## \[/ {exit}
    grab {print}
  ' "$CHANGELOG" > "$notes"

  echo
  ok "prepared v$new: files updated, artifact + notes written to dist/"
  echo "Review the diff, then run:"
  echo
  echo "    git add Alchemist.json CHANGELOG.md"
  echo "    git commit -m \"release: v$new\""
  echo "    git tag -a v$new -m \"v$new\""
  echo "    git push --follow-tags"
  echo
  echo "Then create a GitHub Release for tag v$new, attach $zipfile,"
  echo "and paste $notes as the body. See RELEASING.md."
}

do_doctor() {
  step "doctor"
  local fail=0
  if command -v dotnet >/dev/null;   then ok "dotnet $(dotnet --version 2>/dev/null)"; else bad "dotnet not found; install the .NET 9 SDK (https://dotnet.microsoft.com)"; fail=1; fi
  if have_py;                        then ok "python $("${PY_CMD[@]}" --version 2>&1 | cut -d' ' -f2) (${PY_CMD[*]})"; else bad "no Python >= 3.10, needed for scripts/dev.sh test; install uv (https://astral.sh/uv) to provision one, or install Python directly"; fail=1; fi
  if [ -x "$GODOT" ];                then ok "MegaDot at $GODOT"; else bad "MegaDot not found at $GODOT; install it or set GODOT=/path/to/Godot (see BUILD.md)"; fail=1; fi
  if [ -d "$STS2_GAME_DIR" ];        then ok "game at $STS2_GAME_DIR"; else bad "game not found at $STS2_GAME_DIR; install via Steam or set STS2_GAME_DIR"; fail=1; fi
  if pgrep -x steam_osx >/dev/null 2>&1 || pgrep -x steam >/dev/null 2>&1; then ok "Steam client running"; else bad "Steam client not running, needed to launch the game (game-start/test)"; fi
  if [ -d "$STS2_MCP_DIR" ];         then ok "tooling at $STS2_MCP_DIR"; else bad "sts2-modding-mcp checkout missing; run scripts/dev.sh setup"; fail=1; fi
  if [ -d "$GAME_MODS/Alchemist" ];  then ok "Alchemist mod installed"; else bad "Alchemist mod not installed; run scripts/dev.sh publish"; fail=1; fi
  if [ -d "$GAME_MODS/mcptest" ];    then ok "MCPTest bridge installed"; else bad "MCPTest bridge missing; run scripts/dev.sh bridge"; fail=1; fi
  if [ -d "$GAME_MODS/godotexplorer" ]; then ok "GodotExplorer installed"; else bad "GodotExplorer missing; run scripts/dev.sh bridge"; fail=1; fi
  if port_open 21337; then ok "MCPTest bridge responding on :21337"; else bad "MCPTest bridge not reachable on :21337; launch the game via Steam (needed only for 'test')"; fi
  if port_open 27020; then ok "GodotExplorer responding on :27020"; else bad "GodotExplorer not reachable on :27020; launch the game via Steam (needed only for 'test')"; fi
  [ "$fail" -eq 0 ] && { echo; ok "environment looks good"; } || { echo; bad "fix the items above, then re-run scripts/dev.sh doctor"; }
  return "$fail"
}

case "${1:-help}" in
  setup)         "$REPO/scripts/setup.sh" ;;
  publish)       do_build; do_import; do_publish; do_verify ;;
  publish-fast)  do_build; do_publish; do_verify ;;
  import)        do_import ;;
  bridge)        do_bridge ;;
  test)          shift; do_test "$@" ;;
  game-start)    do_game start ;;
  game-stop)     do_game stop ;;
  game-restart)  do_game restart ;;
  lint)          have_py || { bad "$no_py_msg"; exit 1; }
                 "${PY_CMD[@]}" "$REPO/scripts/lint_sync.py" ;;
  changelog)     do_changelog ;;
  release)       shift; do_release "$@" ;;
  doctor)        do_doctor ;;
  env)
    echo "REPO          = $REPO"
    echo "STS2_GAME_DIR = $STS2_GAME_DIR"
    echo "GAME_MODS     = $GAME_MODS"
    echo "STS2_MCP_DIR  = $STS2_MCP_DIR"
    echo "GODOT         = $GODOT"
    echo "PCK           = $PCK"
    ;;
  *)
    grep -E '^#( |$)' "${BASH_SOURCE[0]}" | sed -E 's/^# ?//'
    ;;
esac
