#!/usr/bin/env python3
"""Run the Alchemist regression suite against the live game — no agents required.

Usage (normally via `scripts/dev.sh test [args]`):
    PYTHONPATH=<sts2-modding-mcp> python3 scripts/tests/run_suite.py [options] [name...]

Options:
    --group NAME    run only this group (a subdirectory of scripts/tests/)
    --fresh         restart the game before running (when state is suspect)
    --speed N       game speed while running (default 3 — fastest visually stable)
    --game start|stop|restart   just manage the game process and exit
    name...         substring filters on scenario filenames

Scenarios are JSON files in group subdirectories (cards/, ancients/, shop/,
settings/, compendium/). Two kinds:
  - "steps" scenarios run through sts2mcp.test_runner (combat-oriented).
  - "checks" scenarios run in this file's checks engine (see README.md for the
    vocabulary) and assert by polling until true, so they pass the moment the
    game settles instead of sleeping fixed delays.

The runner reuses a healthy running game (restarting per run is too slow at
scale); it starts the game if it's not running, and restarts it if it looks
crashed or wedged. The game boots into the last-used save profile — keep that
pointed at a spare profile.

Exit code 0 iff all selected scenarios pass.
"""

import json
import os
import subprocess
import sys
import time
from pathlib import Path

TESTS_DIR = Path(__file__).parent
REPO_DIR = TESTS_DIR.parent.parent
STEAM_URL = "steam://rungameid/2868840"
GAME_PROCESS = "Slay the Spire 2"

# ── locate the sts2-modding-mcp checkout ────────────────────────────────────
def _find_mcp_dir() -> Path | None:
    candidates = [
        os.environ.get("STS2_MCP_DIR"),
        REPO_DIR / ".tooling" / "sts2-modding-mcp",
        Path.home() / "code" / "sts2-modding-mcp",
    ]
    for c in candidates:
        if c and (Path(c) / "sts2mcp" / "test_runner.py").is_file():
            return Path(c)
    return None


_mcp_dir = _find_mcp_dir()
if _mcp_dir is None:
    print("✗ sts2-modding-mcp checkout not found — run scripts/setup.sh first")
    sys.exit(2)
sys.path.insert(0, str(_mcp_dir))

from sts2mcp import bridge_client, godot_explorer_client, test_runner  # noqa: E402

MAIN_MENU_BUTTON = (
    "/root/Game/RootSceneContainer/Run/GlobalUi/OverlayScreensContainer"
    "/GameOverScreen/ScreenshakeContainer/Ui/MainMenuButton"
)
DIALOGUE_LABEL = (
    "/root/Game/RootSceneContainer/Run/RoomContainer/EventRoom/EventContainer"
    "/AncientEventLayout/ContentContainer/Content/DialogueContainer"
    "/AncientDialogueLine/DialogueContainer/TextContainer/Text"
)
# can't be played in a solo run
MULTIPLAYER_CARDS = {"Bestow", "Effervesce", "Reflux", "Suffuse"}


def _r(response: dict) -> dict:
    """Unwrap a bridge JSON-RPC envelope to its result payload."""
    return response.get("result", response) if isinstance(response, dict) else {}


# ── game lifecycle ───────────────────────────────────────────────────────────

def game_running() -> bool:
    return subprocess.run(
        ["pgrep", "-f", GAME_PROCESS], capture_output=True
    ).returncode == 0


def steam_running() -> bool:
    for name in ("steam_osx", "steam"):
        if subprocess.run(["pgrep", "-x", name], capture_output=True).returncode == 0:
            return True
    return False


def bridge_ready() -> bool:
    return "error" not in _r(bridge_client.ping())


def start_game(timeout: float = 120.0) -> bool:
    """Launch via the Steam protocol and wait until the bridge answers at the menu."""
    if not steam_running():
        print("✗ Steam is not running — start Steam first (game launches through it)")
        return False
    if not game_running():
        opener = "open" if sys.platform == "darwin" else "xdg-open"
        subprocess.Popen([opener, STEAM_URL])
        print("  launching game via Steam ...")
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if bridge_ready():
            screen = str(_r(bridge_client.get_screen()).get("screen", ""))
            if screen == "MAIN_MENU":
                return True
        time.sleep(2)
    print(f"✗ game did not reach the main menu within {timeout:.0f}s")
    return False


def stop_game(timeout: float = 20.0) -> bool:
    """Quit gracefully (Apple Event), escalate to SIGTERM if it lingers."""
    if not game_running():
        return True
    if sys.platform == "darwin":
        # the game exits but osascript often reports "User canceled" — ignore rc
        subprocess.run(
            ["osascript", "-e", f'tell application "{GAME_PROCESS}" to quit'],
            capture_output=True,
        )
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if not game_running():
            return True
        time.sleep(1)
    subprocess.run(["pkill", "-f", GAME_PROCESS], capture_output=True)
    time.sleep(3)
    return not game_running()


def ensure_game_ready(fresh: bool = False) -> bool:
    """Reuse a healthy running game; start if absent; restart only if unhealthy."""
    if fresh and game_running():
        print("  --fresh: restarting the game")
        stop_game()
    if not game_running():
        if not start_game():
            return False
    if not bridge_ready():
        print("  bridge unresponsive — restarting the game")
        stop_game()
        if not start_game():
            return False
    if not reset_to_menu():
        print("  cannot reach the main menu — restarting the game")
        stop_game()
        return start_game()
    return True


# ── in-game reset ────────────────────────────────────────────────────────────

def reset_to_menu(timeout: float = 15.0) -> bool:
    """Return the game to the main menu, ending any in-progress run.

    `die` ends a combat run (an out-of-combat run is abandoned via the pause menu),
    then the game-over screen is dismissed by force-clicking its MainMenuButton —
    the only dismiss path that works without window focus.
    """
    state = _r(bridge_client.get_run_state())
    if state.get("in_progress"):
        screen = str(_r(bridge_client.get_screen()).get("screen", ""))
        if "COMBAT" in screen:
            bridge_client.execute_console_command("die")
        else:
            bridge_client.navigate_menu("abandon")
        time.sleep(1.0)
        godot_explorer_client.call_method(MAIN_MENU_BUTTON, "ForceClick")

    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        screen = str(_r(bridge_client.get_screen()).get("screen", ""))
        if screen == "MAIN_MENU":
            return True
        if "OVERLAY" in screen:  # game-over may still be animating in; retry
            godot_explorer_client.call_method(MAIN_MENU_BUTTON, "ForceClick")
        elif "MENU" in screen:  # a submenu (settings, compendium) is open — pop it
            bridge_client.navigate_menu("back")
        time.sleep(0.5)
    return False


# ── checks engine ────────────────────────────────────────────────────────────
# Scenario form: {"setup": {start_run kwargs}?, "checks": [{do, expect, timeout}...]}
# Each expect polls until every expectation holds or the timeout hits.

class CheckFailure(Exception):
    pass


def _combat() -> dict:
    return _r(bridge_client.get_combat_state())


def _combat_player() -> dict:
    players = _combat().get("players") or [{}]
    return players[0]


def _hand_index(card_name: str, timeout: float = 5.0) -> int:
    """Find a card in hand by class name (injected cards land at the end, but
    indexes shift as cards are played — name lookup is the only robust handle)."""
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        for c in _combat_player().get("hand") or []:
            if c.get("name") == card_name:
                return int(c["index"])
        time.sleep(0.3)
    raise CheckFailure(f"card {card_name!r} not in hand")


def _play_card_by_name(card_name: str, target: int | None = None) -> None:
    idx = _hand_index(card_name)
    hand = _combat_player().get("hand") or []
    card = next(c for c in hand if c.get("index") == idx)
    tgt = -1
    if card.get("target_type") == "AnyEnemy":
        if target is not None:
            tgt = target
        else:
            enemies = _combat().get("enemies") or []
            tgt = next((e["index"] for e in enemies if e.get("is_alive")), 0)
    bridge_client.send_request("play_card", {"card_index": idx, "target_index": tgt})


def _do(action: dict, ctx: dict) -> None:
    if "console" in action:
        bridge_client.execute_console_command(action["console"])
    elif "play_card" in action:
        _play_card_by_name(action["play_card"], action.get("target"))
    elif "upgrade_card" in action:
        idx = _hand_index(action["upgrade_card"])
        bridge_client.execute_console_command(f"upgrade {idx}")
    elif "end_turn" in action:
        bridge_client.send_request("end_turn", {})
    elif "set_enemy_hp" in action:
        # normalize an enemy to a fixed HP so damage asserts are roster-independent;
        # console damage/heal use 1-based creature indexes (0 = player)
        spec = action["set_enemy_hp"]
        enemies = _combat().get("enemies") or []
        enemy = next((e for e in enemies if e.get("index") == spec["enemy"]), None)
        if enemy is None:
            return  # fewer enemies than the scenario normalizes — tolerate it
        delta = int(enemy["hp"]) - int(spec["hp"])
        if delta > 0:
            bridge_client.execute_console_command(f"damage {delta} {spec['enemy'] + 1}")
        elif delta < 0:
            bridge_client.execute_console_command(f"heal {-delta} {spec['enemy'] + 1}")
    elif "bridge" in action:
        bridge_client.send_request(action["bridge"], action.get("params") or {})
    elif "click" in action:
        godot_explorer_client.call_method(action["click"], "ForceClick")
    elif "click_method" in action:  # "node/path|MethodName" — call an arbitrary node method
        path, method = action["click_method"].rsplit("|", 1)
        godot_explorer_client.call_method(path, method)
    elif "find_click" in action:
        # Locate a node whose runtime path can't be hardcoded (Godot @-generated names
        # differ per session): find by name pattern, filter by a path substring, then
        # optionally descend to the first child of a given class (generated config
        # buttons wrap the clickable in an anonymous child Control).
        spec = action["find_click"]
        nodes = godot_explorer_client.find_nodes(spec["pattern"])
        path = next(
            (n["path"] for n in nodes.get("results", [])
             if spec.get("contains", "") in n.get("path", "")
             and "GodotExplorer" not in n.get("path", "")),
            None)
        if path is None:
            raise CheckFailure(f"find_click: no node matches {spec}")
        if spec.get("child_class"):
            tree = godot_explorer_client.get_scene_tree(root_path=path, depth=1)
            path = next(
                (c["path"] for c in tree.get("children", [])
                 if c.get("class") == spec["child_class"]),
                path)
        godot_explorer_client.call_method(path, "ForceClick")
    elif "menu" in action:
        bridge_client.navigate_menu(action["menu"])
    elif "use_potion" in action:
        bridge_client.use_potion(int(action["use_potion"]), action.get("target", -1))
    elif "use_potion_ui" in action:
        _use_potion_ui(int(action["use_potion_ui"]))
    elif "discard_potion" in action:
        bridge_client.discard_potion(int(action["discard_potion"]))
    elif "advance_ancient" in action:
        # complete an open ancient event onto the map: click its "Proceed" option
        acts = _r(bridge_client.get_available_actions()).get("actions", [])
        idx = next((a["choice_index"] for a in acts
                    if a.get("action") == "event_option"
                    and str(a.get("label", "")).startswith("Proceed")), None)
        if idx is not None:
            bridge_client.make_event_choice(idx)
    elif "walk_dialogue" in action:
        _walk_ancient_dialogue(ctx)
    elif "click_label" in action:
        # ForceClick a button under `root` whose child Label reads `label` — for
        # option rows the bridge doesn't surface (rest-site Brew, etc.)
        spec = action["click_label"]
        path = _find_by_label(spec["root"], spec["label"])
        if path is None:
            raise CheckFailure(f"click_label: no '{spec['label']}' under {spec['root']}")
        godot_explorer_client.call_method(path, "ForceClick")
    elif "remove_deck_card" in action:
        # deck card-removal screen (Brew): select a card, then click its preview-confirm
        bridge_client.execute_action("card_select", card_index=int(action["remove_deck_card"]))
        time.sleep(1.0)
        godot_explorer_client.call_method(
            "/root/Game/RootSceneContainer/Run/GlobalUi/OverlayScreensContainer"
            "/NDeckCardSelectScreen/PreviewContainer/PreviewConfirm", "ForceClick")
    elif "reward_select" in action:
        bridge_client.execute_action("reward_select", reward_index=int(action["reward_select"]))
    elif "snapshot" in action:
        if action["snapshot"] == "gold":
            ctx["gold"] = _player().get("gold")
        elif action["snapshot"] == "hp":
            ctx["hp"] = _player().get("hp")
    elif "sleep" in action:
        time.sleep(float(action["sleep"]))
    else:
        raise CheckFailure(f"unknown do: {action}")


def _walk_ancient_dialogue(ctx: dict) -> None:
    """Advance an open ancient event through EVERY line of its dialogue the way a player
    does — clicking the invisible 'next' hitbox — and verify each line renders on screen
    (no raw keys/errors) and that clicking actually walks the sequence to the last line.

    Without this a scenario only ever sees the first line before the run is abandoned, so
    lines 2..N are never exercised on screen. Standard AncientEventLayout events only (the
    Architect's finale uses a combat layout and is covered by the dialogue registry checks)."""
    layout = DIALOGUE_LABEL.split("/ContentContainer")[0]
    container = DIALOGUE_LABEL.split("/AncientDialogueLine")[0]

    def _prop(path: str, name: str) -> str:
        raw = str(godot_explorer_client.get_property(path, name))
        return raw.split(" = ", 1)[1].strip() if " = " in raw else raw.strip()

    def _lines() -> list[str]:
        tree = godot_explorer_client.get_scene_tree(root_path=container, depth=1)
        return [c["path"] for c in (tree.get("children") or [])] if isinstance(tree, dict) else []

    # Dialogue line nodes are add_child'd deferred (materialize over later frames) and the
    # 'next' hitbox is only enabled once setup completes — wait for the line count to settle
    # so we don't walk a half-built conversation.
    lines, deadline = _lines(), time.monotonic() + 5
    while time.monotonic() < deadline:
        time.sleep(0.3)
        nxt = _lines()
        if nxt and nxt == lines:
            break
        lines = nxt
    if not lines:
        raise CheckFailure(f"walk_dialogue: no dialogue lines under {container}")

    hits = godot_explorer_client.find_nodes("DialogueHitbox")
    hitbox = next((n["path"] for n in (hits.get("results") or [])
                   if "EventRoom" in n.get("path", "") and "GodotExplorer" not in n.get("path", "")), None)

    fails: list[str] = []
    n = len(lines)
    for i, line in enumerate(lines):
        txt = _prop(f"{line}/DialogueContainer/TextContainer/Text", "text")
        if not txt or "LocString table" in txt or txt.lower().startswith("error"):
            fails.append(f"line {i} renders (got {txt[:40]!r})")
        if _prop(layout, "_currentDialogueLine") != str(i):
            fails.append(f"line {i}: sequence sits on it (at {_prop(layout, '_currentDialogueLine')!r})")
        if i < n - 1:
            if hitbox is None:
                raise CheckFailure("walk_dialogue: 'next' hitbox not found")
            # click 'next' and wait for the sequence to advance; only re-click while still on
            # line i (never past line i+1) so a slow click can't be double-counted.
            adv = time.monotonic() + 3
            while time.monotonic() < adv:
                cur = _prop(layout, "_currentDialogueLine")
                if cur == str(i + 1):
                    break
                if cur == str(i):
                    godot_explorer_client.call_method(hitbox, "ForceClick")
                time.sleep(0.2)
            if _prop(layout, "_currentDialogueLine") != str(i + 1):
                fails.append(f"clicking 'next' from line {i} advanced the dialogue")
                break
    if "true" not in _prop(layout, "IsDialogueOnLastLine").lower():
        fails.append("dialogue reached its last line (options never enabled)")
    ctx["dialogue_lines_walked"] = n
    if fails:
        raise CheckFailure(f"walk_dialogue ({n} lines): " + "; ".join(fails))


_POTION_HOLDERS = ("/root/Game/RootSceneContainer/Run/GlobalUi/TopBar/LeftAlignedStuff"
                   "/PotionMarginifier/PotionContainer/MarginContainer/PotionHolders")


def _use_potion_ui(slot: int = 0) -> None:
    """Drink/throw the potion in belt `slot` via its popup — the bridge's use_potion
    reports success but no-ops (doesn't consume or apply the potion)."""
    tree = godot_explorer_client.get_scene_tree(root_path=_POTION_HOLDERS, depth=1)
    holders = [c["path"] for c in tree.get("children", [])] if isinstance(tree, dict) else []
    holder = holders[slot] if slot < len(holders) else holders[0]
    godot_explorer_client.call_method(holder, "OpenPotionPopup")
    time.sleep(1.2)
    godot_explorer_client.call_method(f"{holder}/PotionPopup/Container/UseButton", "ForceClick")
    time.sleep(1.0)


def _find_by_label(root: str, label: str) -> str | None:
    """Path of the direct child of `root` whose own child Label reads `label`."""
    tree = godot_explorer_client.get_scene_tree(root_path=root, depth=1)
    for child in tree.get("children", []) if isinstance(tree, dict) else []:
        txt = godot_explorer_client.get_property(child["path"] + "/Label", "text")
        if isinstance(txt, str) and txt.replace("text = ", "").strip() == label:
            return child["path"]
    return None


def _player() -> dict:
    players = _r(bridge_client.get_player_state()).get("players") or [{}]
    return players[0]


def _action_labels() -> list[str]:
    actions = _r(bridge_client.get_available_actions()).get("actions") or []
    return [str(a.get("label", "")) for a in actions if a.get("label")]


def _find_pool(kind: str, pool_name: str) -> dict | None:
    comp = _r(bridge_client.get_compendium())
    for pool in comp.get(kind) or []:
        if pool.get("pool") == pool_name or pool_name in str(pool.get("id", "")):
            return pool
    return None


def _pool_member_names(pool_name: str) -> tuple[list[str], str] | None:
    for kind, member_key in (
        ("card_pools", "cards"), ("relic_pools", "relics"), ("potion_pools", "potions"),
    ):
        pool = _find_pool(kind, pool_name)
        if pool is not None:
            return [m.get("name", "") for m in pool.get(member_key) or []], member_key
    return None


def _csv_expected_names(exclude_rarities: list[str], exclude_names: list[str]) -> list[str]:
    """Expected card class names derived from cards.csv (the design sheet)."""
    import csv as _csv
    special = {"Strike": "StrikeAlchemist", "Defend": "DefendAlchemist"}
    names = []
    with open(REPO_DIR / "cards.csv", newline="") as f:
        for row in list(_csv.reader(f))[1:]:
            if len(row) < 2 or not row[0].strip():
                continue
            display, rarity = row[0].strip(), row[1]
            if any(x.lower() in rarity.lower() for x in exclude_rarities):
                continue
            cls = special.get(display, display.replace(" ", "").replace("'", ""))
            if cls not in exclude_names:
                names.append(cls)
    return names


def _eval_expect(expect: dict, ctx: dict) -> list[str]:
    """Return a list of failure strings (empty = all expectations hold)."""
    fails = []
    for key, want in expect.items():
        if key == "screen":
            got = str(_r(bridge_client.get_screen()).get("screen", ""))
            if got != want:
                fails.append(f"screen == {want!r} (actual: {got!r})")
        elif key == "screen_contains":
            got = str(_r(bridge_client.get_screen()).get("screen", ""))
            if want not in got:
                fails.append(f"screen contains {want!r} (actual: {got!r})")
        elif key == "actions_labels_exclude":
            bad = [l for l in _action_labels() if want in l]
            if bad:
                fails.append(f"no action label contains {want!r} (found: {bad[:3]})")
        elif key == "actions_label_contains":
            labels = _action_labels()
            if not any(want in l for l in labels):
                fails.append(f"some action label contains {want!r} (labels: {labels[:6]})")
        elif key == "actions_count_gte":
            n = len(_r(bridge_client.get_available_actions()).get("actions") or [])
            if n < want:
                fails.append(f"actions_count >= {want} (actual: {n})")
        elif key == "gold":
            got = _player().get("gold")
            if got != want:
                fails.append(f"gold == {want} (actual: {got})")
        elif key == "gold_delta":
            base = ctx.get("gold")
            got = _player().get("gold")
            if base is None or got is None or got - base != want:
                fails.append(f"gold_delta == {want} (base: {base}, actual: {got})")
        elif key == "potion_count":
            potions = _player().get("potions") or []
            n = sum(1 for p in potions if p.get("name") not in (None, "", "empty"))
            if n != want:
                fails.append(f"potion_count == {want} (actual: {n})")
        elif key == "deck_count":
            got = _player().get("deck_count")
            if got != want:
                fails.append(f"deck_count == {want} (actual: {got})")
        elif key == "hp_gain_gte":  # hp risen by >= want since a `snapshot: "hp"`
            base = ctx.get("hp")
            got = _player().get("hp")
            if base is None or got is None or got - base < want:
                fails.append(f"hp gained >= {want} (base: {base}, actual: {got})")
        elif key == "rest_option":  # a rest-site option button with this label exists
            if _find_by_label(want["root"], want["label"]) is None:
                fails.append(f"rest option labeled {want['label']!r} exists")
        elif key == "node_text":
            raw = godot_explorer_client.get_property(want["path"], "text")
            got = str(raw)
            if isinstance(raw, str) and raw.startswith("text = "):
                got = raw[len("text = "):]
            if "contains" in want and want["contains"] not in got:
                fails.append(f"node text contains {want['contains']!r} (actual: {got!r})")
            if "excludes" in want and want["excludes"] in got:
                fails.append(f"node text excludes {want['excludes']!r} (actual: {got!r})")
        elif key == "hp":
            got = _player().get("hp")
            if got != want:
                fails.append(f"hp == {want} (actual: {got})")
        elif key in ("energy", "block", "hand_size"):
            cp = _combat_player()
            got = len(cp.get("hand") or []) if key == "hand_size" else cp.get(key)
            if got != want:
                fails.append(f"{key} == {want} (actual: {got})")
        elif key == "power":
            powers = {p.get("name"): p.get("amount") for p in _combat_player().get("powers") or []}
            got = powers.get(want["name"])
            if got != want["amount"]:
                fails.append(f"power {want['name']} == {want['amount']} (actual: {got})")
        elif key == "has_power":
            powers = [p.get("name") for p in _combat_player().get("powers") or []]
            if want not in powers:
                fails.append(f"has power {want!r} (powers: {powers})")
        elif key == "hand_contains":  # a card of this class name is in hand
            names = [c.get("name") for c in _combat_player().get("hand") or []]
            if want not in names:
                fails.append(f"hand contains {want!r} (hand: {names})")
        elif key == "powers":  # list of {name, amount} — for asserting several at once
            have = {p.get("name"): p.get("amount") for p in _combat_player().get("powers") or []}
            for spec in want:
                if have.get(spec["name"]) != spec["amount"]:
                    fails.append(f"power {spec['name']} == {spec['amount']} (actual: {have.get(spec['name'])})")
        elif key == "enemy_hp":
            enemies = _combat().get("enemies") or []
            enemy = next((e for e in enemies if e.get("index") == want["enemy"]), None)
            got = enemy.get("hp") if enemy else None
            if got != want["hp"]:
                fails.append(f"enemy {want['enemy']} hp == {want['hp']} (actual: {got})")
        elif key == "any_enemy_hp":
            # index-independent: AoE that kills small enemies compacts the array, so the
            # surviving normalized enemy moves index. Assert on the HP value instead.
            hps = [e.get("hp") for e in _combat().get("enemies") or [] if e.get("is_alive")]
            if want not in hps:
                fails.append(f"some alive enemy has hp == {want} (alive hps: {hps})")
        elif key == "dialogue_loc_complete":
            # General, source-derived: for every ancient the mod writes Alchemist dialogue
            # for (grouped from ancients.json), the live registered dialogue must render all
            # lines (no raw keys/errors) and match the loc file on both the number of
            # conversations (visit-gated dialogues) and the total spoken-line count. Together
            # these prove every ancient's full set of visit conversations is registered and
            # renders. Auto-adapts when dialogue is added/removed — no per-ancient expectations.
            import re as _re
            import collections as _co
            loc = json.loads((REPO_DIR / "Alchemist/localization/eng/ancients.json").read_text())
            loc_lines = _co.Counter()
            loc_convs: dict[str, set] = _co.defaultdict(set)
            for k in loc:
                m = _re.match(r"([A-Z_]+)\.talk\.ALCHEMIST-ALCHEMIST\.(\d+)-\d+r?\.(char|ancient)$", k)
                if m:
                    loc_lines[m.group(1)] += 1
                    loc_convs[m.group(1)].add(int(m.group(2)))
            rpc = {a["ancient"]: a for a in _r(bridge_client.send_request("get_ancient_dialogues")).get("ancients") or []}
            norm = {n.replace("_", "").upper(): a for n, a in rpc.items()}
            for prefix, n_lines in sorted(loc_lines.items()):
                a = norm.get(prefix.replace("_", ""))
                n_convs = len(loc_convs[prefix])
                if a is None:
                    fails.append(f"{prefix}: no registered dialogue for the Alchemist")
                elif a.get("bad_lines"):
                    fails.append(f"{prefix}: unrendered lines {a['bad_lines'][:2]}")
                elif a.get("dialogue_count") != n_convs:
                    fails.append(f"{prefix}: {a.get('dialogue_count')} registered conversations vs {n_convs} in loc")
                elif a.get("line_count") != n_lines:
                    fails.append(f"{prefix}: {a.get('line_count')} registered lines vs {n_lines} in loc")
        elif key == "ancient_dialogues":
            data = _r(bridge_client.get_ancient_dialogues())
            entry = next((a for a in data.get("ancients") or []
                          if a.get("ancient") == want["ancient"]), None)
            if entry is None:
                fails.append(f"ancient {want['ancient']!r} found (not in dialogue registry)")
            else:
                n = entry.get("dialogue_count", 0)
                if n < want.get("min_dialogues", 1):
                    fails.append(f"{want['ancient']}: >= {want.get('min_dialogues', 1)} Alchemist dialogues (actual: {n})")
                bad = entry.get("bad_lines") or []
                if bad:
                    fails.append(f"{want['ancient']}: all dialogue lines render (bad: {bad[:3]})")
        elif key == "dialogue_on_screen":
            raw = str(godot_explorer_client.get_property(DIALOGUE_LABEL, "text"))
            got = raw[len("text = "):] if raw.startswith("text = ") else raw
            if not got.strip() or "LocString table" in got or "error" in got.lower()[:20]:
                fails.append(f"dialogue label shows localized text (actual: {got[:60]!r})")
        elif key == "loc_render_clean":
            prefix = want.get("prefix", "ALCHEMIST")
            comp = _r(bridge_client.get_compendium())
            problems = []
            for pool in comp.get("card_pools") or []:
                for m in pool.get("cards") or []:
                    if prefix in str(m.get("id", "")):
                        for f in ("title", "description"):
                            v = m.get(f, "")
                            if "LocString table" in v or "!!ERROR" in v or not v.strip():
                                problems.append(f"card {m['name']}.{f}: {v[:50]!r}")
            for kind, mk in (("relic_pools", "relics"), ("potion_pools", "potions")):
                for pool in comp.get(kind) or []:
                    for m in pool.get(mk) or []:
                        if prefix in str(m.get("id", "")):
                            for f in ("title", "description"):
                                v = m.get(f, "")
                                if "LocString table" in v or "!!ERROR" in v or not v.strip():
                                    problems.append(f"{mk[:-1]} {m['name']}.{f}: {v[:50]!r}")
            for pw in comp.get("powers") or []:
                if prefix in str(pw.get("id", "")):
                    for f in ("title", "description", "smart_description"):
                        v = pw.get(f, "")
                        if "LocString table" in v or "!!ERROR" in v or not v.strip():
                            problems.append(f"power {pw['name']}.{f}: {v[:50]!r}")
            # de-dupe (cards appear in multiple pools)
            problems = sorted(set(problems))
            if problems:
                fails.append(f"{len(problems)} loc render problem(s): " + "; ".join(problems[:5]))
        elif key == "exceptions_clean":
            since = ctx.get("exceptions_since", 0)
            exc = _r(bridge_client.get_exceptions(since_id=since)).get("exceptions") or []
            if exc:
                first = exc[0].get("message", "")[:120]
                fails.append(f"no new exceptions (got {len(exc)}: {first})")
        elif key == "game_log_contains":
            since = ctx.get("log_since", 0)
            log = _r(bridge_client.get_game_log(contains=want, max_count=50))
            hits = [e for e in log.get("entries") or [] if e.get("id", 0) > since]
            if not hits:
                fails.append(f"game log contains {want!r} (no new matching entries)")
        elif key == "epoch_state":
            # Assert timeline epoch + gated-content state (via the bridge's get_epoch_state).
            # Shape: {prefix, epochs/cards/relics/potions: {model_id: {field: expected, …}}}.
            # Epoch fields: state/visible/revealed. Content fields: unlocked/discovered.
            st = _r(bridge_client.get_epoch_state(want.get("prefix", "")))
            by_id = {g: {i["id"]: i for i in (st.get(g) or [])}
                     for g in ("epochs", "cards", "relics", "potions")}
            for group in ("epochs", "cards", "relics", "potions"):
                for mid, expected in (want.get(group) or {}).items():
                    got = by_id[group].get(mid)
                    if got is None:
                        fails.append(f"epoch_state: {group} {mid} present")
                        continue
                    for field, exp in expected.items():
                        if got.get(field) != exp:
                            fails.append(f"epoch_state: {mid}.{field} == {exp} (actual: {got.get(field)})")
        elif key == "pool_contains":
            found = _pool_member_names(want["pool"])
            if found is None:
                fails.append(f"pool {want['pool']!r} exists (not found)")
            else:
                missing = [n for n in want["names"] if n not in found[0]]
                if missing:
                    fails.append(f"pool {want['pool']} contains all (missing: {missing})")
        elif key == "pool_count":
            found = _pool_member_names(want["pool"])
            if found is None:
                fails.append(f"pool {want['pool']!r} exists (not found)")
            else:
                n = len(found[0])
                if "gte" in want and n < want["gte"]:
                    fails.append(f"pool {want['pool']} count >= {want['gte']} (actual: {n})")
                if "count" in want and n != want["count"]:
                    fails.append(f"pool {want['pool']} count == {want['count']} (actual: {n})")
        elif key == "pool_matches_csv":
            found = _pool_member_names(want["pool"])
            if found is None:
                fails.append(f"pool {want['pool']!r} exists (not found)")
            else:
                expected = _csv_expected_names(
                    want.get("exclude_rarities") or [], want.get("exclude_names") or [])
                missing = sorted(set(expected) - set(found[0]))
                if missing:
                    fails.append(f"pool {want['pool']} missing csv cards: {missing}")
        else:
            fails.append(f"unknown expect key: {key}")
    return fails


def run_checks_scenario(scenario: dict, skip_setup: bool = False) -> dict:
    result = {"scenario_name": scenario.get("name"), "passed": True, "failures": []}
    ctx: dict = {}
    # baselines for exceptions_clean / game_log_contains
    exc = _r(bridge_client.get_exceptions()).get("exceptions") or []
    ctx["exceptions_since"] = max((e.get("id", 0) for e in exc), default=0)
    ctx["log_since"] = _r(bridge_client.get_game_log(max_count=1)).get("latest_id", 0)

    # skip_setup: a batched combat scenario — the shared run + fresh SLIMES_WEAK combat
    # was already established by the caller, so don't start a new run.
    setup = scenario.get("setup") or {}
    if setup and not skip_setup:
        bridge_client.start_run(**setup)
        bridge_client.wait_for_screen(
            "COMBAT_PLAYER_TURN" if setup.get("fight") else "EVENT", timeout_seconds=30)

    for i, check in enumerate(scenario.get("checks", [])):
        try:
            if "do" in check:
                _do(check["do"], ctx)
            if "delay" in check:
                time.sleep(float(check["delay"]))
            if "expect" in check:
                timeout = float(check.get("timeout", 6))
                deadline = time.monotonic() + timeout
                fails = _eval_expect(check["expect"], ctx)
                while fails and time.monotonic() < deadline:
                    time.sleep(0.3)
                    fails = _eval_expect(check["expect"], ctx)
                if fails:
                    result["passed"] = False
                    note = f" ({check['note']})" if check.get("note") else ""
                    result["failures"] += [f"check {i}{note}: {f}" for f in fails]
                    break
        except Exception as e:  # bridge/explorer error mid-check
            result["passed"] = False
            result["failures"].append(f"check {i}: error {e}")
            break
    return result


# ── sweeps ───────────────────────────────────────────────────────────────────
# Whole-mod smoke passes: exercise every entity, report every failure (not just
# the first), and check for new exceptions after each step.

def _model_entry(model_id: str, name: str) -> str:
    """Console id (ALCHEMIST-SNAKE) from a compendium id string."""
    import re
    m = re.search(r"ALCHEMIST-[A-Z0-9_]+", str(model_id))
    if m:
        return m.group(0)
    return "ALCHEMIST-" + "".join(
        ("_" + ch if ch.isupper() and i else ch) for i, ch in enumerate(name)).upper()


def _new_exceptions(ctx: dict) -> tuple[list[str], list[str]]:
    """Return (mod_failures, other) exception messages since the last call.

    mod_failures = exceptions whose (bridge-unwrapped) type/stack references the
    Alchemist namespace — a real card/mod bug. other = base-game/bridge/harness noise
    (e.g. reset-nav artifacts), reported but not blamed on a card.
    """
    exc = _r(bridge_client.get_exceptions(since_id=ctx.get("exceptions_since", 0))).get("exceptions") or []
    if exc:
        ctx["exceptions_since"] = max(e.get("id", 0) for e in exc)
    mod, other = [], []
    for e in exc:
        blob = (e.get("stack_trace", "") or "") + (e.get("message", "") or "")
        line = f"{e.get('type', '?').split('.')[-1]}: {e.get('message', '')[:100]}"
        (mod if "Alchemist" in blob else other).append(line)
    return mod, other


def _player_hp() -> tuple[int | None, int | None]:
    ps = _player()
    return ps.get("hp"), ps.get("max_hp")


def _potion_count() -> int:
    return sum(1 for p in _player().get("potions") or []
              if p.get("name") not in (None, "", "empty"))


def _sweep_new_run(ctx: dict) -> None:
    """Fresh run from the menu — used at sweep start and to recover after a death."""
    reset_to_menu()
    bridge_client.start_run(character="Alchemist", seed="SWEEP1", fight="SLIMES_WEAK")
    bridge_client.wait_for_screen("COMBAT_PLAYER_TURN", timeout_seconds=30)
    _new_exceptions(ctx)


def _fresh_fight(ctx: dict) -> None:
    """A clean combat for the next card: new fight (empties hand/exhaust, revives
    enemies), then top HP back up only if a prior card actually spent it. NOT godmode —
    godmode pumps Regen to ~1e9, which makes Haemorrhage ('lose HP equal to your Regen')
    drain a billion HP and every Regen-scaling card deal absurd damage. With normal
    Regen those cards are harmless no-ops, which is what the sweep wants (it tests that
    they don't *throw*, not that they do damage)."""
    if not _r(bridge_client.get_run_state()).get("in_progress"):
        _sweep_new_run(ctx)
        return
    bridge_client.execute_console_command("fight SLIMES_WEAK")
    bridge_client.wait_for_screen("COMBAT_PLAYER_TURN", timeout_seconds=20)
    hp, mx = _player_hp()
    if hp is not None and mx and hp < mx:  # heal only when below max, so it's a no-op normally
        bridge_client.execute_console_command("heal 9999")
    _new_exceptions(ctx)  # a fight transition can emit noise; don't blame the next card


def _resolve_overlays(max_steps: int = 10) -> None:
    """Clear whatever selection/reward overlay a played card opened.

    Cards open several kinds of blocking UI: a mandatory in-hand pick (Exhaust/Infuse
    → HAND_SELECT, must select+confirm, no skip), a draw-pile pick (Winnow/Dissolve
    → CARD_SELECTION, select+confirm or skip), or a reward/proceed screen. Handle each
    by its screen; fall back to skip/proceed.
    """
    for _ in range(max_steps):
        screen = str(_r(bridge_client.get_screen()).get("screen", ""))
        if screen == "COMBAT_PLAYER_TURN":
            return
        # Fire exactly the action the current screen wants — firing speculative
        # select/skip actions into a not-ready screen throws IndexOutOfRange inside the
        # game's async handlers (surfaces later as an unobserved-task exception that
        # would falsely fail a card).
        try:
            if screen == "HAND_SELECT":
                bridge_client.execute_action("combat_select_card", card_index=0)
                time.sleep(0.3)
                bridge_client.execute_action("combat_confirm_selection")
            elif screen in ("CARD_SELECTION", "CARD_REWARD"):
                bridge_client.card_select(0, confirm=True)
            elif screen == "COMBAT_PLAYER_TURN":
                return
            else:
                bridge_client.execute_action("proceed")
        except Exception:
            pass
        time.sleep(0.6)


def run_sweep(kind: str, scenario: dict) -> dict:
    result = {"scenario_name": scenario.get("name"), "passed": True, "failures": []}
    ctx: dict = {}
    _new_exceptions(ctx)  # baseline

    comp = _r(bridge_client.get_compendium())
    _sweep_new_run(ctx)

    if kind == "cards":
        pool = next(p for p in comp.get("card_pools") or [] if p.get("pool") == "AlchemistCardPool")
        cards = [c for c in pool.get("cards") or [] if c["name"] not in MULTIPLAYER_CARDS]
        played = skipped = 0
        other_exc: list[str] = []
        for card in cards:
            entry = _model_entry(card["id"], card["name"])
            try:
                # Start each card from a clean fight: a fresh combat clears any lingering
                # overlay, empties the hand/exhaust pile (Retain + token-generators would
                # otherwise fill it), and revives enemies AoE cards may have killed.
                _resolve_overlays()
                _fresh_fight(ctx)
                bridge_client.execute_console_command(f"card {entry}")
                try:
                    idx = _hand_index(card["name"], timeout=3)
                except CheckFailure:  # transient injection miss — retry once
                    bridge_client.execute_console_command(f"card {entry}")
                    idx = _hand_index(card["name"], timeout=4)
                cardstate = next(c for c in _combat_player().get("hand") or [] if c.get("index") == idx)
                if not cardstate.get("can_play", True):
                    skipped += 1
                    continue  # needs a condition we didn't set up — not a crash
                hp_before, _ = _player_hp()
                _play_card_by_name(card["name"])
                _resolve_overlays()
                played += 1
                hp_after, _ = _player_hp()
                if hp_after is not None and hp_after <= 0:
                    # a card killing you from full HP is a real bug, not accumulation —
                    # _fresh_fight tops HP up before every card
                    result["failures"].append(
                        f"{card['name']}: killed the player (HP {hp_before} -> {hp_after})")
                    _sweep_new_run(ctx)  # recover the dead run for the next card
                    continue
                mod, other = _new_exceptions(ctx)
                other_exc += other
                if mod:
                    result["failures"].append(f"{card['name']}: {mod[0]}")
            except Exception as e:
                result["failures"].append(f"{card['name']}: {e}")
        result.setdefault("stats", {})["cards_played"] = played
        result["stats"]["cards_skipped"] = skipped
        print(f"    swept: {played} played, {skipped} unplayable-skipped, "
              f"{len(result['failures'])} card-failures, {len(other_exc)} non-mod exceptions")

    elif kind == "relics":
        pool = next(p for p in comp.get("relic_pools") or [] if p.get("pool") == "AlchemistRelicPool")
        for relic in pool.get("relics") or []:
            bridge_client.execute_console_command(
                f"relic add {_model_entry(relic['id'], relic['name'])}")
            time.sleep(0.3)
        _fresh_fight(ctx)  # combat-start hooks fire with all 9 relics
        bridge_client.execute_console_command("card ALCHEMIST-NIGREDO")
        _play_card_by_name("Nigredo")
        bridge_client.send_request("end_turn", {})
        bridge_client.wait_for_screen("COMBAT_PLAYER_TURN", timeout_seconds=20)
        mod, _ = _new_exceptions(ctx)
        if mod:
            result["failures"].append(f"relics: {mod[0]}")

    elif kind == "potions":
        pool = next(p for p in comp.get("potion_pools") or [] if p.get("pool") == "AlchemistPotionPool")
        potions = pool.get("potions") or []
        try:
            _fresh_fight(ctx)
            # add them ALL to the belt first, then use each (the bridge's use_potion is a
            # no-op, so drive the belt popup's Use button — that actually applies them)
            for potion in potions:
                bridge_client.execute_console_command(f"potion {_model_entry(potion['id'], potion['name'])}")
                time.sleep(0.4)
            added = _potion_count()
            if added != len(potions):
                result["failures"].append(f"potions: added {added}/{len(potions)} to belt")
            for slot, potion in enumerate(potions):
                before = _potion_count()
                _use_potion_ui(slot)  # belt slots don't compact — each potion keeps its slot
                _resolve_overlays()
                if _potion_count() >= before:
                    result["failures"].append(f"{potion['name']}: not consumed on use")
                mod, _ = _new_exceptions(ctx)
                if mod:
                    result["failures"].append(f"{potion['name']}: {mod[0]}")
        except Exception as e:
            result["failures"].append(f"potions: {e}")

    result["passed"] = not result["failures"]
    return result


# ── standard scenarios (sts2mcp.test_runner) ─────────────────────────────────

def describe_failures(result: dict) -> list[str]:
    lines = []
    if result.get("error"):
        lines.append(f"    error: {result['error']}")
    for step in result.get("steps", []):
        if step.get("passed"):
            continue
        for check in step.get("assertion_results", []):
            if not check.get("passed"):
                lines.append(
                    f"    step {step.get('step')}: {check.get('assertion')}"
                    f" — actual: {check.get('actual')}"
                )
        if not step.get("assertion_results"):
            lines.append(f"    step {step.get('step')} ({step.get('action')}) failed")
    return lines


# ── discovery / main ─────────────────────────────────────────────────────────

def _combat_batchable(scenario: dict) -> bool:
    """A checks scenario that just needs a fresh SLIMES_WEAK combat (no special screen
    or cross-run state) can share one run with its neighbours — reset via `fight`
    between them instead of a full menu round-trip. Opt out with `"batch": false`."""
    if "checks" not in scenario or scenario.get("batch") is False:
        return False
    setup = scenario.get("setup") or {}
    return (setup.get("fight") == "SLIMES_WEAK"
            and set(setup) <= {"character", "seed", "fight", "ascension"})


def discover(group_filter: str | None, name_filters: list[str]) -> list[Path]:
    paths = sorted(TESTS_DIR.rglob("*.json"))
    if group_filter:
        paths = [p for p in paths if p.parent.name == group_filter]
    if name_filters:
        paths = [p for p in paths if any(f in p.stem.lower() for f in name_filters)]
    return paths


def main() -> int:
    args = sys.argv[1:]

    def take_opt(flag: str) -> str | None:
        if flag in args:
            i = args.index(flag)
            val = args[i + 1]
            del args[i:i + 2]
            return val
        return None

    if "--game" in args:  # lifecycle-only mode for dev.sh game-* commands
        action = take_opt("--game")
        ok = {"start": lambda: ensure_game_ready(),
              "stop": stop_game,
              "restart": lambda: ensure_game_ready(fresh=True)}[action]()
        print(("✓ " if ok else "✗ ") + f"game {action}")
        return 0 if ok else 1

    speed = float(take_opt("--speed") or os.environ.get("ALCH_TEST_SPEED", "3"))
    group = take_opt("--group")
    fresh = "--fresh" in args
    if fresh:
        args.remove("--fresh")
    name_filters = [a.lower() for a in args]

    paths = discover(group, name_filters)
    if not paths:
        print(f"✗ no scenarios match group={group!r} names={name_filters} in {TESTS_DIR}")
        return 2

    if not ensure_game_ready(fresh=fresh):
        print("✗ could not get the game ready — is Steam running? (scripts/dev.sh doctor)")
        return 2
    print("✓ game ready (bridge + explorer connected)")

    no_batch = "--no-batch" in sys.argv
    bridge_client.set_game_speed(speed)
    passed, failed, restarts = [], [], 0
    current_group = None
    in_batch = False  # a shared combat run is currently open for batched scenarios
    ctx_batch: dict = {}
    try:
        for path in paths:
            scenario = json.loads(path.read_text())
            grp = scenario.get("group") or path.parent.name
            if grp != current_group:
                current_group = grp
                print(f"\n── {grp} ──")
            desc = scenario.get("description", "")
            batched = _combat_batchable(scenario) and not no_batch

            # Batched combat scenarios share one run: a fresh SLIMES_WEAK fight between
            # them (no menu round-trip / death animation / Neow) — the big time saver.
            # Everything else resets to the menu and runs with its own setup.
            if batched:
                if not in_batch:
                    reset_to_menu()  # drop any leftover run so the batch starts clean
                    ctx_batch = {}
                _fresh_fight(ctx_batch)
                in_batch = True
            else:
                in_batch = False
                if not reset_to_menu():
                    restarts += 1
                    if restarts > 2 or not ensure_game_ready(fresh=True):
                        print(f"✗ {path.stem}: game wedged and restart failed — aborting")
                        failed.append(path.stem)
                        break
                    bridge_client.set_game_speed(speed)

            t0 = time.monotonic()
            try:
                if "sweep" in scenario:
                    result = run_sweep(scenario["sweep"], scenario)
                    ok = result["passed"]
                    detail = [f"    {f}" for f in result["failures"]]
                elif "checks" in scenario:
                    result = run_checks_scenario(scenario, skip_setup=batched)
                    ok = result["passed"]
                    detail = [f"    {f}" for f in result["failures"]]
                else:
                    result = test_runner.run_test_scenario(scenario)
                    ok = bool(result.get("passed"))
                    detail = describe_failures(result)
            except Exception as e:
                ok, detail = False, [f"    runner error: {e}"]
            took = time.monotonic() - t0

            mark = "✓" if ok else "✗"
            suffix = f" — {desc}" if desc else ""
            print(f"{mark} {path.stem}  ({took:.1f}s){suffix}")
            (passed if ok else failed).append(path.stem)
            if not ok:
                for line in detail:
                    print(line)
    finally:
        try:  # leave the game the way we found it: menu, normal speed
            reset_to_menu()
            bridge_client.set_game_speed(1.0)
        except Exception:
            pass

    print(f"\n{len(passed)} passed, {len(failed)} failed of {len(paths)} scenario(s)")
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
