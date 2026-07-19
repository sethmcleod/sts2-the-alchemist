#!/usr/bin/env bash
# One command that prepares the mod for development and for tests.
#
#   scripts/setup.sh
#
# The steps:
#   1. It clones the sts2-modding-mcp toolkit (bridge mods and test engine) into .tooling/
#   2. It checks the large prerequisites (dotnet, MegaDot, the game). It prints help text for
#      each prerequisite that is missing. It never installs those automatically
#   3. It builds the MCPTest and GodotExplorer bridge mods and installs them into the game
#   4. It runs `dev.sh doctor`, so you get a ✓/✗ list for the full environment
#
# Overrides:
#   STS2_MCP_REPO  git URL of the toolkit           (default: sethmcleod/sts2-modding-mcp)
#   STS2_MCP_DIR   toolkit checkout that exists     (default: .tooling/sts2-modding-mcp)
#   STS2_GAME_DIR  game install dir                 (default: the Steam library of the platform)
#
# After setup, the daily commands are in scripts/dev.sh (publish / test / doctor).
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEV="$REPO/scripts/dev.sh"
STS2_MCP_REPO="${STS2_MCP_REPO:-https://github.com/sethmcleod/sts2-modding-mcp}"
TOOLING_DIR="$REPO/.tooling/sts2-modding-mcp"

step() { printf '\n\033[1;36m▶ %s\033[0m\n' "$*"; }
ok()   { printf '\033[32m✓\033[0m %s\n' "$*"; }
bad()  { printf '\033[31m✗\033[0m %s\n' "$*"; }

# ── 1. toolkit checkout ──────────────────────────────────────────────────────
step "sts2-modding-mcp toolkit"
if [ -n "${STS2_MCP_DIR:-}" ] && [ -d "$STS2_MCP_DIR" ]; then
  ok "use the checkout at \$STS2_MCP_DIR ($STS2_MCP_DIR)"
elif [ -d "$HOME/code/sts2-modding-mcp" ] && [ ! -d "$TOOLING_DIR" ]; then
  ok "use the checkout at ~/code/sts2-modding-mcp"
elif [ -d "$TOOLING_DIR/.git" ]; then
  ok "already cloned at .tooling/sts2-modding-mcp; get the latest changes"
  git -C "$TOOLING_DIR" pull --ff-only || bad "the pull failed (local changes?); continue with the current checkout"
else
  echo "clone $STS2_MCP_REPO → .tooling/sts2-modding-mcp"
  mkdir -p "$REPO/.tooling"
  git clone --depth 1 "$STS2_MCP_REPO" "$TOOLING_DIR"
  ok "cloned"
fi
# keep the tooling clone out of version control
if ! grep -qx '\.tooling/' "$REPO/.gitignore" 2>/dev/null; then
  echo '.tooling/' >> "$REPO/.gitignore"
  ok "added .tooling/ to .gitignore"
fi

# ── 2. prerequisites (detect and give help, no automatic install) ───────────
step "prerequisites"
missing=0
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet/tools"
if command -v dotnet >/dev/null; then
  ok "dotnet $(dotnet --version 2>/dev/null)"
else
  bad "dotnet not found; install the .NET 9 SDK: https://dotnet.microsoft.com/download"
  missing=1
fi
py_found=""
for p in python3.13 python3.12 python3.11 python3.10 python3; do
  if command -v "$p" >/dev/null && "$p" -c 'import sys; sys.exit(0 if sys.version_info >= (3,10) else 1)' 2>/dev/null; then
    py_found="$p"; break
  fi
done
if [ -n "$py_found" ]; then
  ok "python $("$py_found" --version 2>&1 | cut -d' ' -f2) ($py_found)"
elif command -v uv >/dev/null 2>&1; then
  ok "uv $(uv --version 2>&1 | cut -d' ' -f2); it will supply Python for the regression suite"
else
  bad "no Python 3.10 or later found; install uv (https://astral.sh/uv) to get one, or install Python directly"
fi
GODOT="${GODOT:-/Applications/MegaDot.app/Contents/MacOS/Godot}"
if [ -x "$GODOT" ]; then
  ok "MegaDot at $GODOT"
else
  bad "MegaDot (the Godot fork of STS2) not found; 'publish' needs it for the asset export."
  echo "  See the BUILD.md prerequisites; set GODOT=/path/to/Godot if your copy is in a different place."
fi

# ── 3. bridge mods (they need dotnet and the game) ───────────────────────────
if [ "$missing" -eq 0 ]; then
  step "bridge mods (MCPTest + GodotExplorer)"
  "$DEV" bridge
else
  bad "no bridge mod install; first correct the prerequisites above that are missing"
fi

# ── 4. final check ────────────────────────────────────────────────────────────
"$DEV" doctor || true

step "next steps"
cat <<'EOF'
  1. scripts/dev.sh publish     # build the mod and install it into the game
  2. Start Slay the Spire 2 through Steam (use a spare save profile, see scripts/tests/README.md)
  3. scripts/dev.sh test        # run the regression suite against the live game
EOF
