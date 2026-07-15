#!/usr/bin/env bash
# Dev helper for the Alchemist mod — one command for each tedious loop.
#
#   scripts/dev.sh setup          first-time setup: clone tooling, check deps, install bridge mods
#   scripts/dev.sh publish        build → godot import → publish → verify pck   (safe default)
#   scripts/dev.sh publish-fast   build → publish → verify pck                  (code-only, skips import)
#   scripts/dev.sh import         godot --headless --import only
#   scripts/dev.sh bridge         build + install the MCPTest + GodotExplorer bridge mods into the game
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

# The test engine (sts2mcp) needs Python >= 3.10; pick the newest available.
find_python() {
  local p
  for p in python3.13 python3.12 python3.11 python3.10 python3; do
    if command -v "$p" >/dev/null && "$p" -c 'import sys; sys.exit(0 if sys.version_info >= (3,10) else 1)' 2>/dev/null; then
      echo "$p"; return 0
    fi
  done
  return 1
}
PY="$(find_python || true)"

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
# the game mods dir. NOT 0Harmony.dll — the game ships its own; a duplicate conflicts.
install_bridge_mod() {  # <project-subdir> <csproj> <dll-base> <mods-subdir>
  local src="$STS2_MCP_DIR/$1" csproj="$2" base="$3" dest="$GAME_MODS/$4"
  step "build $base"
  [ -d "$src" ] || { bad "missing $src — run scripts/dev.sh setup first"; exit 1; }
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

do_doctor() {
  step "doctor"
  local fail=0
  if command -v dotnet >/dev/null;   then ok "dotnet $(dotnet --version 2>/dev/null)"; else bad "dotnet not found — install the .NET 9 SDK (https://dotnet.microsoft.com)"; fail=1; fi
  if [ -n "$PY" ];                   then ok "python $("$PY" --version 2>&1 | cut -d' ' -f2) ($PY)"; else bad "no Python >= 3.10 — needed for scripts/dev.sh test (e.g. brew install python@3.12)"; fail=1; fi
  if [ -x "$GODOT" ];                then ok "MegaDot at $GODOT"; else bad "MegaDot not found at $GODOT — install it or set GODOT=/path/to/Godot (see BUILD.md)"; fail=1; fi
  if [ -d "$STS2_GAME_DIR" ];        then ok "game at $STS2_GAME_DIR"; else bad "game not found at $STS2_GAME_DIR — install via Steam or set STS2_GAME_DIR"; fail=1; fi
  if [ -d "$STS2_MCP_DIR" ];         then ok "tooling at $STS2_MCP_DIR"; else bad "sts2-modding-mcp checkout missing — run scripts/dev.sh setup"; fail=1; fi
  if [ -d "$GAME_MODS/Alchemist" ];  then ok "Alchemist mod installed"; else bad "Alchemist mod not installed — run scripts/dev.sh publish"; fail=1; fi
  if [ -d "$GAME_MODS/mcptest" ];    then ok "MCPTest bridge installed"; else bad "MCPTest bridge missing — run scripts/dev.sh bridge"; fail=1; fi
  if [ -d "$GAME_MODS/godotexplorer" ]; then ok "GodotExplorer installed"; else bad "GodotExplorer missing — run scripts/dev.sh bridge"; fail=1; fi
  if port_open 21337; then ok "MCPTest bridge responding on :21337"; else bad "MCPTest bridge not reachable on :21337 — launch the game via Steam (needed only for 'test')"; fi
  if port_open 27020; then ok "GodotExplorer responding on :27020"; else bad "GodotExplorer not reachable on :27020 — launch the game via Steam (needed only for 'test')"; fi
  [ "$fail" -eq 0 ] && { echo; ok "environment looks good"; } || { echo; bad "fix the items above, then re-run scripts/dev.sh doctor"; }
  return "$fail"
}

case "${1:-help}" in
  setup)         "$REPO/scripts/setup.sh" ;;
  publish)       do_build; do_import; do_publish; do_verify ;;
  publish-fast)  do_build; do_publish; do_verify ;;
  import)        do_import ;;
  bridge)        do_bridge ;;
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
