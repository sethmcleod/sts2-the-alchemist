#!/usr/bin/env bash
# Dev helper for the Alchemist mod. One command does each repeated loop.
#
#   scripts/dev.sh setup          first-time setup: clone the tooling, check the dependencies,
#                                 install the bridge mods
#   scripts/dev.sh publish        build → godot import → publish → verify pck   (the safe default)
#   scripts/dev.sh publish-fast   build → publish → verify pck                  (code only, no import)
#   scripts/dev.sh import         godot --headless --import only
#   scripts/dev.sh bridge         build the MCPTest and GodotExplorer bridge mods, then install them
#   scripts/dev.sh test [args]    run the regression suite (it starts the game if necessary;
#                                 args: --group NAME, --changed, --changed-since REF, --fresh,
#                                 name filters; see scripts/tests/README.md)
#   scripts/dev.sh game-start     start the game through Steam and wait for the bridge
#   scripts/dev.sh game-stop      quit the game (a normal quit first, then a forced quit)
#   scripts/dev.sh game-restart   stop and start (the game loads the new bridge and mod builds)
#   scripts/dev.sh lint           static check of the three-way rule (offline, no game)
#   scripts/dev.sh changelog      draft CHANGELOG entries from the commits since the last tag
#                                 (it prints only, it writes nothing)
#   scripts/dev.sh release <patch|minor|major|X.Y.Z> [--skip-tests]
#                                 increase the version, roll the CHANGELOG, build, and package
#                                 dist/Alchemist-vX.Y.Z.zip. It prints the git commit/tag/push block
#                                 for you to run (see RELEASING.md). It runs the regression suite, so
#                                 Steam must be up. --skip-tests stops the suite
#   scripts/dev.sh doctor         check every prerequisite and print ✓/✗ with the fixes
#   scripts/dev.sh env            print the resolved paths and exit
#
# The reason for this script: every publish needs PATH and DOTNET_ROLL_FORWARD set. It also needs
# the game dir exported for the bridge build, and 3 or 4 commands in a chain. This script does all
# of that, so the inner loop is one word.
#
# You can override every path below with an environment variable (see the env output). The
# defaults detect the platform, so a fresh clone works and you do not have to edit this file.
set -euo pipefail

# ── the environment that every command needs ────────────────────────────
# dotnet is often not on the default login PATH. RollForward=Major lets the net9 analyzers run on
# newer runtimes. The bridge csproj files read STS2_GAME_DIR to find sts2.dll.
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

# sts2-modding-mcp checkout (bridge mods and test engine): first the environment override,
# then the local clone that `setup` makes, then a shared ~/code checkout.
if [ -z "${STS2_MCP_DIR:-}" ]; then
  if   [ -d "$REPO/.tooling/sts2-modding-mcp" ]; then STS2_MCP_DIR="$REPO/.tooling/sts2-modding-mcp"
  elif [ -d "$HOME/code/sts2-modding-mcp" ];     then STS2_MCP_DIR="$HOME/code/sts2-modding-mcp"
  else STS2_MCP_DIR="$REPO/.tooling/sts2-modding-mcp"   # the location that `setup` uses
  fi
fi

GODOT="${GODOT:-/Applications/MegaDot.app/Contents/MacOS/Godot}"
PCK="$GAME_MODS/Alchemist/Alchemist.pck"

# The test engine (sts2mcp) needs Python 3.10 or later. Use a system python if the PATH has one.
# If the PATH has none, use uv. uv supplies a correct Python for you (this is the best option, see
# BUILD.md). PY_CMD holds the interpreter command as an array, because the uv form has more than
# one word.
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

port_open() { (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null && exec 3>&- && return 0 || return 1; }

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
    echo "  run 'scripts/dev.sh game-restart' before you do a test (see docs/troubleshooting.md)"
  fi
}

# Build one bridge mod project from the tooling checkout. Then copy its runtime files into
# the game mods dir. Do not copy 0Harmony.dll: the game supplies its own copy, and a second
# copy causes a conflict.
install_bridge_mod() {  # <project-subdir> <csproj> <dll-base> <mods-subdir>
  local src="$STS2_MCP_DIR/$1" csproj="$2" base="$3" dest="$GAME_MODS/$4"
  step "build $base"
  [ -d "$src" ] || { bad "$src is missing; run scripts/dev.sh setup first"; exit 1; }
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
  echo "Now start STS2 through Steam. MCPTest listens on TCP 21337, GodotExplorer on 27020."
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

do_release() {  # <patch|minor|major|X.Y.Z> [--skip-tests]
  local bump="" skip_tests=0
  for arg in "$@"; do
    case "$arg" in
      --skip-tests) skip_tests=1 ;;
      *) [ -n "$bump" ] && { bad "unexpected argument '$arg'"; exit 1; }; bump="$arg" ;;
    esac
  done
  [ -n "$bump" ] || { bad "usage: scripts/dev.sh release <patch|minor|major|X.Y.Z> [--skip-tests]"; exit 1; }
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

  # The suite is the only check before a release that runs the mod against a real game. A
  # release without the suite can contain a fault. The suite starts the game itself, so
  # Steam must be active.
  if [ "$skip_tests" -eq 1 ]; then
    bad "this run does NOT include the regression suite (--skip-tests); no test ran against the game"
  else
    do_test || { bad "the regression suite failed; correct the fault, or run again with --skip-tests"; exit 1; }
  fi

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
  echo "Examine the diff, then run these commands:"
  echo
  echo "    git add Alchemist.json CHANGELOG.md"
  echo "    git commit -m \"release: v$new\""
  echo "    git tag -a v$new -m \"v$new\""
  echo "    git push --follow-tags"
  echo
  echo "Then create a GitHub Release for tag v$new. Attach $zipfile"
  echo "and paste $notes as the body. See RELEASING.md."
}

do_doctor() {
  step "doctor"
  local fail=0
  if command -v dotnet >/dev/null;   then ok "dotnet $(dotnet --version 2>/dev/null)"; else bad "dotnet not found; install the .NET 9 SDK (https://dotnet.microsoft.com)"; fail=1; fi
  if have_py;                        then ok "python $("${PY_CMD[@]}" --version 2>&1 | cut -d' ' -f2) (${PY_CMD[*]})"; else bad "no Python 3.10 or later; scripts/dev.sh test needs it; install uv (https://astral.sh/uv) to get one, or install Python directly"; fail=1; fi
  if [ -x "$GODOT" ];                then ok "MegaDot at $GODOT"; else bad "MegaDot not found at $GODOT; install it, or set GODOT=/path/to/Godot (see BUILD.md)"; fail=1; fi
  if [ -d "$STS2_GAME_DIR" ];        then ok "game at $STS2_GAME_DIR"; else bad "game not found at $STS2_GAME_DIR; install it through Steam, or set STS2_GAME_DIR"; fail=1; fi
  if pgrep -x steam_osx >/dev/null 2>&1 || pgrep -x steam >/dev/null 2>&1; then ok "Steam client is active"; else bad "Steam client is not active; the game needs it to start (game-start/test)"; fi
  if [ -d "$STS2_MCP_DIR" ];         then ok "tooling at $STS2_MCP_DIR"; else bad "the sts2-modding-mcp checkout is missing; run scripts/dev.sh setup"; fail=1; fi
  if [ -d "$GAME_MODS/Alchemist" ];  then ok "Alchemist mod installed"; else bad "Alchemist mod not installed; run scripts/dev.sh publish"; fail=1; fi
  if [ -d "$GAME_MODS/mcptest" ];    then ok "MCPTest bridge installed"; else bad "the MCPTest bridge is missing; run scripts/dev.sh bridge"; fail=1; fi
  if [ -d "$GAME_MODS/godotexplorer" ]; then ok "GodotExplorer installed"; else bad "GodotExplorer is missing; run scripts/dev.sh bridge"; fail=1; fi
  if port_open 21337; then ok "the MCPTest bridge answers on :21337"; else bad "the MCPTest bridge does not answer on :21337; start the game through Steam (only 'test' needs it)"; fi
  if port_open 27020; then ok "GodotExplorer answers on :27020"; else bad "GodotExplorer does not answer on :27020; start the game through Steam (only 'test' needs it)"; fi
  [ "$fail" -eq 0 ] && { echo; ok "the environment is correct"; } || { echo; bad "correct the items above, then run scripts/dev.sh doctor again"; }
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
