#!/usr/bin/env bash
# scripts/dev.sh publish                       build, import, publish to the game
# scripts/dev.sh publish-fast                  build, publish (code only)
# scripts/dev.sh lint                          static checks
# scripts/dev.sh release <patch|minor|X.Y.Z|promote>
# scripts/dev.sh release-publish [--force] [--draft] [X.Y.Z]
set -euo pipefail

export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet/tools"
export DOTNET_ROLL_FORWARD=Major

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export STS2_GAME_DIR="${STS2_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}"
GAME_MODS="$STS2_GAME_DIR/SlayTheSpire2.app/Contents/MacOS/mods"
APP_MANIFEST="$(dirname "$(dirname "$STS2_GAME_DIR")")/appmanifest_2868840.acf"
GODOT="${GODOT:-/Applications/MegaDot.app/Contents/MacOS/Godot}"
PCK="$GAME_MODS/Alchemist/Alchemist.pck"
CHANGELOG="$REPO/CHANGELOG.md"
MANIFEST="$REPO/Alchemist.json"
TARGETS="$REPO/workshop/targets.json"
DIST="$REPO/dist"

step() { printf '\n\033[1;36m▶ %s\033[0m\n' "$*"; }
ok()   { printf '\033[32m✓\033[0m %s\n' "$*"; }
bad()  { printf '\033[31m✗\033[0m %s\n' "$*"; }
die()  { bad "$*"; exit 1; }

do_build()   { step "build"; cd "$REPO"; dotnet build -p:Sts2Path="$STS2_GAME_DIR"; }
do_import()  { step "import"; cd "$REPO"; "$GODOT" --headless --import --path . 2>&1 | grep -iE "error|reimport" | grep -viE "EditorSettings|TypeNameResolver" || true; }
do_publish() { step "publish"; cd "$REPO"; dotnet publish -c Debug -p:Sts2Path="$STS2_GAME_DIR"; }
do_verify()  {
  [ -f "$PCK" ] || die "no pck at $PCK"
  ls -lh "$PCK"
  if pgrep -f "Contents/MacOS/Slay the Spire 2" >/dev/null; then
    bad "the game is running and still has the old pck open: restart it"
  fi
}
do_lint() {
  python3 "$REPO/scripts/lint_sync.py"
  python3 "$REPO/scripts/check_card_tokens.py"
  python3 "$REPO/scripts/check_ancient_dialogue.py"
}

branch() { git -C "$REPO" rev-parse --abbrev-ref HEAD; }
game_branch() { [ ! -f "$APP_MANIFEST" ] || sed -nE 's/.*"BetaKey"[[:space:]]+"([^"]*)".*/\1/p' "$APP_MANIFEST" | head -1; }
version() { sed -nE 's/.*"version"[[:space:]]*:[[:space:]]*"v?([0-9]+\.[0-9]+\.[0-9]+)".*/\1/p' "$MANIFEST"; }
target() {
  python3 -c 'import json, sys; t = json.load(open(sys.argv[1])).get(sys.argv[2]); sys.exit(f"no target for branch {sys.argv[2]} in workshop/targets.json") if t is None else print(t.get(sys.argv[3], ""))' \
    "$TARGETS" "$(branch)" "$1"
}
section() {
  awk -v head="$1" 'index($0, "## [" head "]") == 1 {grab=1; next} grab && /^## \[/ {exit} grab {print}' "$CHANGELOG"
}

do_release() {
  local bump="${1:-}"
  [ -n "$bump" ] || die "usage: scripts/dev.sh release <patch|minor|X.Y.Z|promote>"
  [ -z "$(git -C "$REPO" status --porcelain)" ] || die "commit your changes first"
  git -C "$REPO" config merge.ours.driver true

  local suffix item title cur new
  suffix="$(target tagSuffix)"; item="$(target itemId)"; title="$(target title)"
  [ -n "$item" ] || die "no itemId for $(branch) in workshop/targets.json"
  local want have; want="$(target gameBranch)"; have="$(game_branch)"
  [ "$have" = "$want" ] || die "$(branch) needs the game's ${want:-default} branch, but Steam is on ${have:-default}"

  cur="$(version)"
  local IFS=. ; local -a p=($cur); unset IFS
  case "$bump" in
    major)   new="$((p[0]+1)).0.0" ;;
    minor)   new="${p[0]}.$((p[1]+1)).0" ;;
    patch)   new="${p[0]}.${p[1]}.$((p[2]+1))" ;;
    promote) new="$cur" ;;
    *)       new="${bump#v}" ;;
  esac
  [[ "$new" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || die "not a version: $new"
  if [ "$bump" = "promote" ]; then
    ! git -C "$REPO" rev-parse -q --verify "refs/tags/v$new$suffix" >/dev/null || die "v$new$suffix already exists"
    [ -n "$(section "$new" | tr -d '[:space:]')" ] || die "CHANGELOG.md has no notes for $new"
  else
    [ "$(printf '%s\n%s\n' "$cur" "$new" | sort -V | tail -1)" = "$new" ] && [ "$new" != "$cur" ] ||
      die "$new is not newer than $cur"
    [ -n "$(section Unreleased | tr -d '[:space:]')" ] || die "CHANGELOG.md has nothing under Unreleased"
  fi

  do_build
  step "lint"; do_lint
  ok "v$cur -> v$new"

  sed -i '' -E "s/(\"version\"[[:space:]]*:[[:space:]]*\")v?[0-9.]+(\")/\1v$new\2/" "$MANIFEST"
  if ! grep -q "^## \[$new\]" "$CHANGELOG"; then
    awk -v ver="$new" -v date="$(date +%Y-%m-%d)" \
      '/^## \[Unreleased\]/ {print; print ""; print "## [" ver "] - " date; next} {print}' \
      "$CHANGELOG" > "$CHANGELOG.tmp" && mv "$CHANGELOG.tmp" "$CHANGELOG"
  fi

  do_publish

  local tag="v$new$suffix" src="$GAME_MODS/Alchemist" f
  local zip="$DIST/Alchemist-$tag.zip" notes="$DIST/RELEASE_NOTES-$tag.txt"
  step "package $zip"
  rm -rf "$DIST/stage" "$zip"; mkdir -p "$DIST/stage/Alchemist" "$REPO/workshop/content"
  for f in Alchemist.dll Alchemist.json Alchemist.pck; do
    [ -f "$src/$f" ] || die "missing $src/$f"
    cp "$src/$f" "$DIST/stage/Alchemist/"
    cp "$src/$f" "$REPO/workshop/content/"
  done
  (cd "$DIST/stage" && zip -r -q "$zip" Alchemist)
  rm -rf "$DIST/stage"

  section "$new" | awk '
    !started && /^[[:space:]]*$/ {next}
    {started=1}
    function flush() { if (line != "") { print line; line = "" } }
    /^[[:space:]]*-[[:space:]]/  { flush(); line = $0; next }
    /^[[:space:]]*$/             { flush(); print; next }
    /^[[:space:]]/ && line != "" { sub(/^[[:space:]]+/, ""); line = line " " $0; next }
    { flush(); print }
    END { flush() }
  ' > "$notes"

  printf '%s\n' "$item" > "$REPO/workshop/mod_id.txt"
  python3 - "$REPO/workshop/workshop.json" "v$new" "$notes" "$title" \
    "$(target descriptionBanner)" "$(target minBranch)" "$(target maxBranch)" <<'PYEOF'
import json, re, sys
path, ver, notes_file, title, banner, minb, maxb = sys.argv[1:8]
cfg = json.load(open(path, encoding="utf-8"))
notes = re.sub(r"(?m)^#+[ \t]*", "", open(notes_file, encoding="utf-8").read().strip())
cfg["title"] = title
cfg["changeNote"] = f"{ver}\n\n{notes}" if notes else ver
if banner:
    body = re.sub(r"^\[quote\].*?\[/quote\]\s*", "", cfg.get("description", ""), count=1, flags=re.S)
    cfg["description"] = f"{banner}\n\n{body}"
cfg["minBranch"] = minb
cfg["maxBranch"] = maxb
with open(path, "w", encoding="utf-8") as f:
    json.dump(cfg, f, indent=2, ensure_ascii=False)
    f.write("\n")
PYEOF

  echo
  ok "$tag ready. Check the diff, then: scripts/dev.sh release-publish"
  echo "Steam upload:"
  echo "  cd ~/code/sts2-mod-uploader && DOTNET_ROLL_FORWARD=LatestMajor dotnet run -c Release --no-build -- upload --id $item -w $REPO/workshop"
}

do_release_publish() {
  local force=0 draft=0 ver=""
  while [ $# -gt 0 ]; do
    case "$1" in
      --force) force=1 ;;
      --draft) draft=1 ;;
      v[0-9]*|[0-9]*) ver="${1#v}" ;;
      *) die "unknown option $1" ;;
    esac
    shift
  done
  gh auth status >/dev/null 2>&1 || die "gh is not logged in: gh auth login"

  local b tag; b="$(branch)"; tag="v${ver:-$(version)}$(target tagSuffix)"
  local zip="$DIST/Alchemist-$tag.zip" notes="$DIST/RELEASE_NOTES-$tag.txt"
  [ -f "$zip" ] && [ -f "$notes" ] || die "no $zip or notes: run scripts/dev.sh release first"

  if [ -n "$(git -C "$REPO" status --porcelain)" ]; then
    local extra; extra="$(git -C "$REPO" status --porcelain | awk '{print $2}' |
      grep -vxE 'Alchemist\.json|CHANGELOG\.md|workshop/workshop\.json|workshop/mod_id\.txt' || true)"
    [ -z "$extra" ] || die "other changes in the tree: $(echo $extra)"
    step "commit release: $tag"
    git -C "$REPO" add Alchemist.json CHANGELOG.md
    if git -C "$REPO" ls-files --error-unmatch workshop/workshop.json >/dev/null 2>&1; then
      git -C "$REPO" add workshop/workshop.json
    fi
    git -C "$REPO" commit -q -m "release: $tag"
  fi

  [ "$(git -C "$REPO" log -1 --format=%s)" = "release: $tag" ] ||
    bad "the newest commit is not 'release: $tag', so the tag goes on it anyway"

  step "tag and push $tag"
  local head tagged
  head="$(git -C "$REPO" rev-parse HEAD)"
  tagged="$(git -C "$REPO" rev-parse -q --verify "refs/tags/$tag^{commit}" || true)"
  if [ -z "$tagged" ]; then
    git -C "$REPO" tag -a "$tag" -m "$tag"
  elif [ "$tagged" != "$head" ]; then
    [ "$force" -eq 1 ] || die "$tag is on another commit: pass --force to move it"
    git -C "$REPO" tag -f -a "$tag" -m "$tag" >/dev/null
  fi
  if [ "$force" -eq 1 ]; then
    git -C "$REPO" push --force-with-lease origin "$b"
    git -C "$REPO" push --force origin "refs/tags/$tag"
  else
    git -C "$REPO" push origin "$b" "refs/tags/$tag"
  fi

  local pre; pre="$(target prerelease)"
  local -a flags=(--title "$tag" --notes-file "$notes")
  if [ "$draft" -eq 1 ]; then flags+=(--draft); fi
  step "GitHub release $tag"
  if gh release view "$tag" >/dev/null 2>&1; then
    if [ "$pre" = "True" ]; then flags+=(--prerelease); else flags+=(--prerelease=false); fi
    gh release edit "$tag" "${flags[@]}" >/dev/null
    gh release upload "$tag" "$zip" --clobber
  else
    if [ "$pre" = "True" ]; then flags+=(--prerelease); fi
    gh release create "$tag" "$zip" "${flags[@]}" >/dev/null
  fi
  ok "$(gh release view "$tag" --json url -q .url)"
}

case "${1:-}" in
  publish)         do_build; do_import; do_publish; do_verify ;;
  publish-fast)    do_build; do_publish; do_verify ;;
  lint)            do_lint ;;
  release)         shift; do_release "$@" ;;
  release-publish) shift; do_release_publish "$@" ;;
  *)               grep -E '^# ' "${BASH_SOURCE[0]}" | sed 's/^# //' ;;
esac
