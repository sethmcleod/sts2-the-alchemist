#!/usr/bin/env bash
# One-command setup for mod development & testing.
#
#   scripts/setup.sh
#
# What it does:
#   1. Clones the sts2-modding-mcp toolkit (bridge mods + test engine) into .tooling/
#   2. Checks the heavier prerequisites (dotnet, MegaDot, the game) and prints install
#      guidance for anything missing; it never tries to auto-install those
#   3. Builds + installs the MCPTest and GodotExplorer bridge mods into the game
#   4. Runs `dev.sh doctor` so you end with a ✓/✗ board of the whole environment
#
# Overrides:
#   STS2_MCP_REPO  git URL of the toolkit           (default: sethmcleod/sts2-modding-mcp)
#   STS2_MCP_DIR   existing toolkit checkout to use (default: .tooling/sts2-modding-mcp)
#   STS2_GAME_DIR  game install dir                 (default: platform Steam library)
#
# After setup, day-to-day commands live in scripts/dev.sh (publish / test / doctor).
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
  ok "using existing checkout at \$STS2_MCP_DIR ($STS2_MCP_DIR)"
elif [ -d "$HOME/code/sts2-modding-mcp" ] && [ ! -d "$TOOLING_DIR" ]; then
  ok "using existing checkout at ~/code/sts2-modding-mcp"
elif [ -d "$TOOLING_DIR/.git" ]; then
  ok "already cloned at .tooling/sts2-modding-mcp, pulling latest"
  git -C "$TOOLING_DIR" pull --ff-only || bad "pull failed (local changes?); continuing with current checkout"
else
  echo "cloning $STS2_MCP_REPO → .tooling/sts2-modding-mcp"
  mkdir -p "$REPO/.tooling"
  git clone --depth 1 "$STS2_MCP_REPO" "$TOOLING_DIR"
  ok "cloned"
fi
# keep the tooling clone out of version control
if ! grep -qx '\.tooling/' "$REPO/.gitignore" 2>/dev/null; then
  echo '.tooling/' >> "$REPO/.gitignore"
  ok "added .tooling/ to .gitignore"
fi

# ── 2. prerequisites (detect + guide, no auto-install) ──────────────────────
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
  ok "uv $(uv --version 2>&1 | cut -d' ' -f2), will provision Python for the regression suite"
else
  bad "no Python >= 3.10 found; install uv (https://astral.sh/uv) to provision one, or install Python directly"
fi
GODOT="${GODOT:-/Applications/MegaDot.app/Contents/MacOS/Godot}"
if [ -x "$GODOT" ]; then
  ok "MegaDot at $GODOT"
else
  bad "MegaDot (STS2's Godot fork) not found, needed for 'publish' (asset export)."
  echo "  See BUILD.md prerequisites; set GODOT=/path/to/Godot if yours lives elsewhere."
fi

# ── 3. bridge mods (needs dotnet + the game) ─────────────────────────────────
if [ "$missing" -eq 0 ]; then
  step "bridge mods (MCPTest + GodotExplorer)"
  "$DEV" bridge
else
  bad "skipping bridge mod install until the missing prerequisites above are fixed"
fi

# ── 4. final check ────────────────────────────────────────────────────────────
"$DEV" doctor || true

step "next steps"
cat <<'EOF'
  1. scripts/dev.sh publish     # build the mod and install it into the game
  2. Launch Slay the Spire 2 via Steam (use a spare save profile, see scripts/tests/README.md)
  3. scripts/dev.sh test        # run the regression suite against the live game
EOF
