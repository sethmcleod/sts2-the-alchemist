#!/usr/bin/env python3
"""Run the Alchemist regression suite against the live game. It needs no agents.

Usage (usually with `scripts/dev.sh test [args]`):
    PYTHONPATH=<sts2-modding-mcp> python3 scripts/tests/run_suite.py [options] [name...]

Options:
    --group NAME    run only this group (a subdirectory of scripts/tests/)
    --fresh         restart the game first (use this when the game state is not certain)
    --speed N       the game speed for the run (default 3, the fastest stable speed)
    --game start|stop|restart   control the game process only, then exit
    name...         substring filters for the scenario filenames

The scenarios are JSON files in the group subdirectories (cards/, ancients/, shop/,
settings/, compendium/). There are two types:
  - a "steps" scenario runs through sts2mcp.test_runner (it is for combat).
  - a "checks" scenario runs in the checks engine of this file (see README.md for the
    vocabulary). It reads the state again and again until each expectation is true.
    Thus it passes as soon as the game is stable, and it does not wait for a fixed time.

The runner uses a game that is already active and healthy, because a restart for each
run is too slow. It starts the game if no game is active. It restarts the game if the
game has a crash or does not answer. The game starts in the save profile of the last
session, so keep that profile set to a spare profile.

The exit code is 0 only if every selected scenario passes.
"""

import json
import os
import re
import subprocess
import sys
import time
from pathlib import Path

TESTS_DIR = Path(__file__).parent
REPO_DIR = TESTS_DIR.parent.parent
STEAM_URL = "steam://rungameid/2868840"
GAME_PROCESS = "Slay the Spire 2"

# ── find the sts2-modding-mcp checkout ──────────────────────────────────────
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
    print("✗ sts2-modding-mcp checkout not found; run scripts/setup.sh first")
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
# a player cannot play these in a solo run
MULTIPLAYER_CARDS = {"Bestow", "Effervesce", "Reflux", "Suffuse"}


def _r(response: dict) -> dict:
    """Get the result payload from a bridge JSON-RPC envelope."""
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
    """Start the game with the Steam protocol. Then wait until the bridge answers at the menu."""
    if not steam_running():
        print("✗ Steam is not active; start Steam first (the game starts through it)")
        return False
    if not game_running():
        opener = "open" if sys.platform == "darwin" else "xdg-open"
        subprocess.Popen([opener, STEAM_URL])
        print("  start the game through Steam ...")
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
    """Send a quit request (Apple Event). Send SIGTERM if the game does not stop."""
    if not game_running():
        return True
    if sys.platform == "darwin":
        # the game exits, but osascript often reports "User canceled", so ignore rc
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


def apply_perf(speed: float, fast_mode: str) -> None:
    """Make the game run the suite at the maximum speed. Apply this again after each start.

    The game process holds the two settings, so a restart returns them to their earlier values
    and all the tests after it become slow. TimeScale returns to 1x, and FastMode returns to
    the value in the profile on disk. The two settings work together. TimeScale makes the waits
    of the game shorter. Instant makes Cmd.Wait and CustomScaledWait return with no wait at
    all, which saves more time.
    """
    bridge_client.set_game_speed(speed)
    res = _r(bridge_client.send_request("set_fast_mode", {"mode": fast_mode}))
    if res.get("error"):
        print(f"  ! could not set fast mode ({res['error']}); the run will be slower")


def ensure_game_ready(fresh: bool = False) -> bool:
    """Use an active healthy game; start one if none is active; restart one that is not healthy."""
    if fresh and game_running():
        print("  --fresh: restart the game")
        stop_game()
    if not game_running():
        if not start_game():
            return False
    if not bridge_ready():
        print("  the bridge does not answer; restart the game")
        stop_game()
        if not start_game():
            return False
    if not reset_to_menu():
        print("  cannot reach the main menu; restart the game")
        stop_game()
        return start_game()
    return True


# ── in-game reset ────────────────────────────────────────────────────────────

def reset_to_menu(timeout: float = 15.0) -> bool:
    """Return the game to the main menu and end a run that is in progress.

    `die` ends a run that is in combat. To end a run that is out of combat, the pause menu
    abandons it. Then a force click on the MainMenuButton closes the game-over screen. This
    is the only method that works when the window does not have the focus.
    """
    state = _r(bridge_client.get_run_state())
    if state.get("in_progress"):
        screen = str(_r(bridge_client.get_screen()).get("screen", ""))
        if "COMBAT" not in screen:
            # Start a combat, so that `die` can end the run. The other method is the abandon
            # option in the pause menu, but real clicks drive that option. A modal that is
            # still open (for example, the potion popup of a shop) blocks those clicks and
            # gives no message. The reset then needs a game restart of about 40 seconds.
            # `die` comes from the console and works with any screen.
            bridge_client.execute_console_command("fight SLIMES_WEAK")
            bridge_client.wait_for_screen("COMBAT_PLAYER_TURN", timeout_seconds=10)
        bridge_client.execute_console_command("die")
        # This is long enough for the game-over screen to exist. The loop below clicks again
        # if the screen was not ready, so this wait does not need to cover the full animation.
        time.sleep(0.3)
        godot_explorer_client.call_method(MAIN_MENU_BUTTON, "ForceClick")

    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        screen = str(_r(bridge_client.get_screen()).get("screen", ""))
        if screen == "MAIN_MENU":
            return True
        if "OVERLAY" in screen:  # the game-over screen can be still in its animation; try again
            godot_explorer_client.call_method(MAIN_MENU_BUTTON, "ForceClick")
        elif "MENU" in screen or screen == "TIMELINE":
            # A submenu (settings, compendium) is open, so close it. TIMELINE comes here
            # because a run that ends with epochs to show goes through the Timeline before
            # the menu. Any test that reveals epochs (Unlock All in the mod config) makes
            # this the usual result.
            bridge_client.navigate_menu("back")
        time.sleep(0.15)
    return False


# ── checks engine ────────────────────────────────────────────────────────────
# Scenario form: {"setup": {start_run kwargs}?, "checks": [{do, expect, timeout}...]}
# Each expect reads the state again and again until every expectation is true or the timeout ends.

class CheckFailure(Exception):
    pass


def _combat() -> dict:
    return _r(bridge_client.get_combat_state())


def _combat_player() -> dict:
    players = _combat().get("players") or [{}]
    return players[0]


def _hand_index(card_name: str, timeout: float = 5.0) -> int:
    """Find a card in the hand by its class name.

    The console adds a card at the end of the hand, but the indexes change as the player
    plays cards. Thus a search by name is the only reliable method.
    """
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        for c in _combat_player().get("hand") or []:
            if c.get("name") == card_name:
                return int(c["index"])
        # The console adds the card asynchronously, so the first search almost always fails,
        # and every caller waits this interval. The state read itself takes one frame and
        # sets the speed of the loop.
        time.sleep(0.05)
    raise CheckFailure(f"card {card_name!r} is not in the hand")


def _select_hand_card(name: str, timeout: float = 6.0) -> None:
    """Select a card by class name in a hand-selection prompt during combat (Infuse and others).

    Always select by name. The index of combat_select_card points into NPlayerHand.ActiveHolders,
    the visual card fan. The order of that fan is not the order of the logical hand from
    get_combat_state. Thus an index from the combat state can select a different card. The
    prompt also needs a short time to list a card that an earlier prompt selected. This is
    the reason for the retry.
    """
    deadline = time.monotonic() + timeout
    offered: list[str] = []
    while time.monotonic() < deadline:
        res = _r(bridge_client.execute_action("combat_select_card", card_name=name))
        offered = res.get("cards") or offered
        if res.get("card") == name:
            return
        time.sleep(0.2)
    raise CheckFailure(f"{name!r} did not become selectable in the prompt (offered: {offered})")


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
        # set an enemy to a fixed HP, so the damage assertions do not depend on the enemy list;
        # the console damage and heal commands use 1-based creature indexes (0 = the player)
        spec = action["set_enemy_hp"]
        enemies = _combat().get("enemies") or []
        enemy = next((e for e in enemies if e.get("index") == spec["enemy"]), None)
        if enemy is None:
            return  # there are fewer enemies than the scenario expects, so accept this
        delta = int(enemy["hp"]) - int(spec["hp"])
        if delta > 0:
            bridge_client.execute_console_command(f"damage {delta} {spec['enemy'] + 1}")
        elif delta < 0:
            bridge_client.execute_console_command(f"heal {-delta} {spec['enemy'] + 1}")
    elif "bridge" in action:
        bridge_client.send_request(action["bridge"], action.get("params") or {})
    elif "reveal_timeline" in action:
        # Reveal epochs through the real Timeline UI, not by a direct save write. This is the only
        # path that runs an epoch's QueueUnlocks and reaches AddEpochSlots, so it is the only way a
        # test can see a duplicate tile. The Timeline screen must already be open.
        # Shape: {"reveal_timeline": {}} for every pending epoch, or {"id": ..., "timeout": s}.
        spec = action["reveal_timeline"] or {}
        try:
            outcome = bridge_client.run_timeline_reveal(
                spec.get("id"), timeout=float(spec.get("timeout", 30))
            )
        except (RuntimeError, TimeoutError) as e:
            raise CheckFailure(f"reveal_timeline: {e}") from e
        last = outcome.get("last") or {}
        if last.get("manual_action_required"):
            raise CheckFailure(
                "reveal_timeline: epochs have no timeline tile and cannot be revealed through the "
                f"UI: {last.get('pending_epoch_ids')}"
            )
        ctx["revealed_epochs"] = outcome.get("revealed") or []
    elif "click" in action:
        godot_explorer_client.call_method(action["click"], "ForceClick")
    elif "click_method" in action:  # "node/path|MethodName", call any method of a node
        path, method = action["click_method"].rsplit("|", 1)
        godot_explorer_client.call_method(path, method)
    elif "find_click" in action:
        # Find a node when you cannot write its run-time path in the code, because the
        # Godot @-generated names are different in each session. Search by name pattern,
        # then filter by a path substring. Then, if necessary, go down to the first child
        # of a given class. A config button that the code generates holds the clickable
        # node in an anonymous child Control.
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
        # finish an open ancient event and return to the map: click its "Proceed" option
        acts = _r(bridge_client.get_available_actions()).get("actions", [])
        idx = next((a["choice_index"] for a in acts
                    if a.get("action") == "event_option"
                    and str(a.get("label", "")).startswith("Proceed")), None)
        if idx is not None:
            bridge_client.make_event_choice(idx)
    elif "walk_dialogue" in action:
        _walk_ancient_dialogue(ctx)
    elif "click_label" in action:
        # ForceClick a button under `root` when its child Label shows `label`. Use this
        # for the option rows that the bridge does not report (Brew at a rest site, and others)
        spec = action["click_label"]
        path = _find_by_label(spec["root"], spec["label"])
        if path is None:
            raise CheckFailure(f"click_label: no '{spec['label']}' under {spec['root']}")
        godot_explorer_client.call_method(path, "ForceClick")
    elif "remove_deck_card" in action:
        # the screen that removes a card from the deck (Brew): select a card, then click its preview-confirm
        bridge_client.execute_action("card_select", card_index=int(action["remove_deck_card"]))
        time.sleep(1.0)
        godot_explorer_client.call_method(
            "/root/Game/RootSceneContainer/Run/GlobalUi/OverlayScreensContainer"
            "/NDeckCardSelectScreen/PreviewContainer/PreviewConfirm", "ForceClick")
    elif "reward_select" in action:
        bridge_client.execute_action("reward_select", reward_index=int(action["reward_select"]))
    elif "select_hand_cards" in action:
        # Control a HAND_SELECT prompt (Infuse and others): select the cards by class name, then confirm.
        names = action["select_hand_cards"]
        if isinstance(names, str):
            names = [names]
        bridge_client.wait_for_screen("HAND_SELECT", timeout_seconds=10)
        for name in names:
            _select_hand_card(name)
            time.sleep(0.3)
        bridge_client.execute_action("combat_confirm_selection")
        time.sleep(0.5)
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
    """Move an open ancient event through EVERY line of its dialogue in the same way as a
    player: click the invisible 'next' hitbox. Check that each line renders on screen with
    no raw keys and no errors. Check also that the clicks move the sequence to the last line.

    Without this function, a scenario sees only the first line before it abandons the run,
    and lines 2 to N never appear on screen. This function supports standard
    AncientEventLayout events only. The final event of the Architect uses a combat layout,
    and the dialogue registry checks cover it."""
    layout = DIALOGUE_LABEL.split("/ContentContainer")[0]
    container = DIALOGUE_LABEL.split("/AncientDialogueLine")[0]

    def _prop(path: str, name: str) -> str:
        raw = str(godot_explorer_client.get_property(path, name))
        return raw.split(" = ", 1)[1].strip() if " = " in raw else raw.strip()

    def _lines() -> list[str]:
        tree = godot_explorer_client.get_scene_tree(root_path=container, depth=1)
        return [c["path"] for c in (tree.get("children") or [])] if isinstance(tree, dict) else []

    # The code adds the dialogue line nodes with a deferred add_child, so they appear over
    # later frames. The 'next' hitbox becomes enabled only after the setup completes. Wait
    # until the line count is stable, so that the conversation is complete before you move
    # through it.
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
            fails.append(f"line {i}: the sequence is on this line (at {_prop(layout, '_currentDialogueLine')!r})")
        if i < n - 1:
            if hitbox is None:
                raise CheckFailure("walk_dialogue: 'next' hitbox not found")
            # click 'next' and wait for the sequence to move forward. Click again only while
            # the sequence is on line i, and never after line i+1. Thus a slow click cannot
            # count two times.
            adv = time.monotonic() + 3
            while time.monotonic() < adv:
                cur = _prop(layout, "_currentDialogueLine")
                if cur == str(i + 1):
                    break
                if cur == str(i):
                    godot_explorer_client.call_method(hitbox, "ForceClick")
                time.sleep(0.2)
            if _prop(layout, "_currentDialogueLine") != str(i + 1):
                fails.append(f"a click on 'next' from line {i} moved the dialogue forward")
                break
    if "true" not in _prop(layout, "IsDialogueOnLastLine").lower():
        fails.append("the dialogue reached its last line (the options did not become enabled)")
    ctx["dialogue_lines_walked"] = n
    if fails:
        raise CheckFailure(f"walk_dialogue ({n} lines): " + "; ".join(fails))


_POTION_HOLDERS = ("/root/Game/RootSceneContainer/Run/GlobalUi/TopBar/LeftAlignedStuff"
                   "/PotionMarginifier/PotionContainer/MarginContainer/PotionHolders")


def _use_potion_ui(slot: int = 0) -> None:
    """Drink or throw the potion in belt `slot` with its popup.

    The use_potion function of the bridge reports success, but it does nothing: it does not
    remove the potion and it does not apply the potion.
    """
    tree = godot_explorer_client.get_scene_tree(root_path=_POTION_HOLDERS, depth=1)
    holders = [c["path"] for c in tree.get("children", [])] if isinstance(tree, dict) else []
    holder = holders[slot] if slot < len(holders) else holders[0]
    godot_explorer_client.call_method(holder, "OpenPotionPopup")
    # Wait until the button of the popup exists. Do not estimate the time of its animation.
    # A ForceClick on a node that does not exist yet does nothing, gives no message, and the
    # game never uses the potion.
    use_button = f"{holder}/PotionPopup/Container/UseButton"
    deadline = time.monotonic() + 5.0
    while time.monotonic() < deadline:
        tree = godot_explorer_client.get_scene_tree(root_path=use_button, depth=0)
        if isinstance(tree, dict) and not tree.get("error"):
            break
        time.sleep(0.1)
    godot_explorer_client.call_method(use_button, "ForceClick")
    time.sleep(0.4)


def _find_by_label(root: str, label: str) -> str | None:
    """The path of the direct child of `root` when its own child Label shows `label`."""
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
    """The card class names that come from cards.csv (the design sheet)."""
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
    """Return a list of failure strings (an empty list means every expectation is true)."""
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
        elif key == "hp_gain_gte":  # the hp increased by >= want since a `snapshot: "hp"`
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
        elif key == "powers":  # a list of {name, amount}, to assert more than one power at once
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
            # no index: an AoE card that kills the small enemies makes the array shorter,
            # so the enemy that survives gets a different index. Assert on the HP value.
            hps = [e.get("hp") for e in _combat().get("enemies") or [] if e.get("is_alive")]
            if want not in hps:
                fails.append(f"some alive enemy has hp == {want} (alive hps: {hps})")
        elif key == "dialogue_loc_complete":
            # This check is general and it comes from the source. The mod writes Alchemist
            # dialogue for a set of ancients (the groups come from ancients.json). For each of
            # those ancients, the registered dialogue in the game must render every line with
            # no raw keys and no errors. It must also agree with the loc file on two numbers:
            # the count of conversations (each one needs a number of visits) and the total
            # count of spoken lines. Together these two numbers prove that the game registers
            # and renders the full set of visit conversations of each ancient. The check
            # adapts when you add dialogue or remove dialogue, and it needs no expectation
            # for each ancient.
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
                    fails.append(f"{prefix}: lines that do not render {a['bad_lines'][:2]}")
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
            # remove the duplicates (a card can be in more than one pool)
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
                fails.append(f"game log contains {want!r} (no new entries that match)")
        elif key == "epoch_state":
            # Assert the state of a Timeline epoch and of the content that it gates (with
            # the get_epoch_state function of the bridge).
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
                    fails.append(f"pool {want['pool']} does not have these csv cards: {missing}")
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

    # skip_setup: this scenario is part of a batch. The caller already made the shared run
    # and, for a combat batch, a new SLIMES_WEAK fight. Thus do not start a new run.
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
        except Exception as e:  # an error from the bridge or the explorer during a check
            result["passed"] = False
            result["failures"].append(f"check {i}: error {e}")
            break
    return result


# ── sweeps ───────────────────────────────────────────────────────────────────
# Quick passes over the full mod: use every entity, report every failure (not only the
# first failure), and look for new exceptions after each step.

def _model_entry(model_id: str, name: str) -> str:
    """Console id (ALCHEMIST-SNAKE) from a compendium id string."""
    m = re.search(r"ALCHEMIST-[A-Z0-9_]+", str(model_id))
    if m:
        return m.group(0)
    return "ALCHEMIST-" + "".join(
        ("_" + ch if ch.isupper() and i else ch) for i, ch in enumerate(name)).upper()


def _new_exceptions(ctx: dict) -> tuple[list[str], list[str]]:
    """Return the (mod_failures, other) exception messages since the last call.

    mod_failures holds the exceptions with a type or a stack that names the Alchemist
    namespace. That is, each one is a real fault in a card or in the mod. other holds the
    messages from the base game, the bridge, or the test harness (for example, the results
    of the reset navigation). The function reports these, but a card is not their cause.
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
    """A new run from the menu. The sweep uses this at its start and after a death."""
    reset_to_menu()
    bridge_client.start_run(character="Alchemist", seed="SWEEP1", fight="SLIMES_WEAK")
    bridge_client.wait_for_screen("COMBAT_PLAYER_TURN", timeout_seconds=30)
    _new_exceptions(ctx)


def _fresh_fight(ctx: dict) -> None:
    """A clean combat for the next card.

    A new fight empties the hand and the exhaust pile, and it returns the enemies to life.
    The function then heals the player, but only if an earlier card used HP. It does not use
    godmode. godmode raises Regen to about 1e9. Hemorrhage ('lose HP equal to your Regen')
    would then remove a billion HP, and every card that scales with Regen would do a very
    large amount of damage. With a normal Regen value those cards have no effect, and that
    is the correct result for the sweep. The sweep tests that a card does not *throw*, not
    that a card does damage."""
    if not _r(bridge_client.get_run_state()).get("in_progress"):
        _sweep_new_run(ctx)
        return
    bridge_client.execute_console_command("fight SLIMES_WEAK")
    bridge_client.wait_for_screen("COMBAT_PLAYER_TURN", timeout_seconds=20)
    hp, mx = _player_hp()
    if hp is not None and mx and hp < mx:  # heal only below the maximum, so this usually does nothing
        bridge_client.execute_console_command("heal 9999")
    _new_exceptions(ctx)  # a change of fight can make extra exceptions; the next card is not their cause


def _resolve_overlays(max_steps: int = 10) -> None:
    """Close the selection overlay or the reward overlay that a card opened.

    A card can open several types of UI that block the game. The first type is a necessary
    selection in the hand (Exhaust and Infuse → HAND_SELECT; you must select and confirm,
    and you cannot skip). The second type is a selection in the draw pile (Winnow and
    Dissolve → CARD_SELECTION; you select and confirm, or you skip). The third type is a
    reward screen or a proceed screen. Handle each type by its screen. For a different
    screen, use skip or proceed.
    """
    for _ in range(max_steps):
        screen = str(_r(bridge_client.get_screen()).get("screen", ""))
        if screen == "COMBAT_PLAYER_TURN":
            return
        # Send only the action that the current screen needs. If you send a select action
        # or a skip action to a screen that is not ready, the async handlers of the game
        # throw IndexOutOfRange. That exception appears later as an unobserved task, and it
        # makes a correct card fail.
        try:
            if screen == "HAND_SELECT":
                bridge_client.execute_action("combat_select_card", card_index=0)
                time.sleep(0.1)
                bridge_client.execute_action("combat_confirm_selection")
            elif screen in ("CARD_SELECTION", "CARD_REWARD"):
                bridge_client.card_select(0, confirm=True)
            elif screen == "COMBAT_PLAYER_TURN":
                return
            else:
                bridge_client.execute_action("proceed")
        except Exception:
            pass
        time.sleep(0.2)


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
                # Start each card with a clean fight. A new combat closes an overlay that
                # is still open, empties the hand and the exhaust pile (Retain and the cards
                # that make tokens fill them), and returns to life the enemies that an AoE
                # card killed.
                _resolve_overlays()
                _fresh_fight(ctx)
                bridge_client.execute_console_command(f"card {entry}")
                try:
                    idx = _hand_index(card["name"], timeout=3)
                except CheckFailure:  # the console did not add the card, so try one more time
                    bridge_client.execute_console_command(f"card {entry}")
                    idx = _hand_index(card["name"], timeout=4)
                cardstate = next(c for c in _combat_player().get("hand") or [] if c.get("index") == idx)
                if not cardstate.get("can_play", True):
                    skipped += 1
                    continue  # the card needs a condition that we did not prepare, not a crash
                hp_before, _ = _player_hp()
                _play_card_by_name(card["name"])
                _resolve_overlays()
                played += 1
                hp_after, _ = _player_hp()
                if hp_after is not None and hp_after <= 0:
                    # a card that kills the player from full HP is a real fault, not the
                    # result of many cards; _fresh_fight heals the player before each card
                    result["failures"].append(
                        f"{card['name']}: killed the player (HP {hp_before} -> {hp_after})")
                    _sweep_new_run(ctx)  # make a new run for the next card, because the player died
                    continue
                mod, other = _new_exceptions(ctx)
                other_exc += other
                if mod:
                    result["failures"].append(f"{card['name']}: {mod[0]}")
            except Exception as e:
                result["failures"].append(f"{card['name']}: {e}")
        result.setdefault("stats", {})["cards_played"] = played
        result["stats"]["cards_skipped"] = skipped
        print(f"    swept: {played} played, {skipped} skipped as unplayable, "
              f"{len(result['failures'])} card failures, {len(other_exc)} non-mod exceptions")

    elif kind == "relics":
        pool = next(p for p in comp.get("relic_pools") or [] if p.get("pool") == "AlchemistRelicPool")
        relics = [(r["name"], _model_entry(r["id"], r["name"])) for r in pool.get("relics") or []]

        def _nigredo_turn() -> list[str]:
            """One combat turn with the relics that the player holds now. It returns the new mod exceptions."""
            _fresh_fight(ctx)  # the combat-start hooks run with the relics that the player holds now
            bridge_client.execute_console_command("card ALCHEMIST-NIGREDO")
            _play_card_by_name("Nigredo")
            bridge_client.send_request("end_turn", {})
            bridge_client.wait_for_screen("COMBAT_PLAYER_TURN", timeout_seconds=20)
            return _new_exceptions(ctx)[0]

        # The first pass is separate: one relic at a time, so a throw names the relic that caused it
        for name, entry in relics:
            bridge_client.execute_console_command(f"relic add {entry}")
            time.sleep(0.3)
            try:
                mod = _nigredo_turn()
                if mod:
                    result["failures"].append(f"{name}: {mod[0]}")
            except Exception as e:
                result["failures"].append(f"{name}: {e}")
            finally:
                bridge_client.execute_console_command(f"relic remove {entry}")
                time.sleep(0.3)

        # Then use all of them together. This is the only pass that can show an interaction
        # between relics. A failure here cannot name one relic, so the message says this.
        for _, entry in relics:
            bridge_client.execute_console_command(f"relic add {entry}")
            time.sleep(0.3)
        try:
            mod = _nigredo_turn()
            if mod:
                result["failures"].append(
                    f"all {len(relics)} relics together (each one passed alone, so an "
                    f"interaction is the probable cause): {mod[0]}")
        except Exception as e:
            result["failures"].append(f"all {len(relics)} relics together: {e}")

        result.setdefault("stats", {})["relics_swept"] = len(relics)
        print(f"    swept: {len(relics)} relics one at a time and 1 combined pass, "
              f"{len(result['failures'])} failure(s)")

    elif kind == "potions":
        pool = next(p for p in comp.get("potion_pools") or [] if p.get("pool") == "AlchemistPotionPool")
        potions = pool.get("potions") or []
        try:
            _fresh_fight(ctx)
            # first add ALL of them to the belt, then use each one. The use_potion function
            # of the bridge does nothing, so use the Use button of the belt popup. That
            # button applies the potion.
            for potion in potions:
                bridge_client.execute_console_command(f"potion {_model_entry(potion['id'], potion['name'])}")
                time.sleep(0.4)
            added = _potion_count()
            if added != len(potions):
                result["failures"].append(f"potions: added {added}/{len(potions)} to the belt")
            for slot, potion in enumerate(potions):
                before = _potion_count()
                _use_potion_ui(slot)  # the belt slots do not move; each potion keeps its slot
                _resolve_overlays()
                if _potion_count() >= before:
                    result["failures"].append(f"{potion['name']}: the game did not remove it on use")
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
                    f" (actual: {check.get('actual')})"
                )
        if not step.get("assertion_results"):
            lines.append(f"    step {step.get('step')} ({step.get('action')}) failed")
    return lines


# ── discovery / main ─────────────────────────────────────────────────────────

def _batch_mode(scenario: dict) -> str | None:
    """Show if a scenario can share a run with the scenarios near it, and how.

    A return to the menu costs about 3.2 seconds (the death, then the load of the main-menu
    scene). The start of a run costs about 3.2 seconds more. That is longer than most
    scenarios. A shared run removes both costs.

      "combat" - the runner infers this: the scenario wants a plain SLIMES_WEAK fight. A new
                 `fight` resets that fight between scenarios, so nothing remains
      "run"    - you must ask for this with `"batch": "run"`: the scenario reaches its own
                 room with the console. It does not depend on the state of the run, and it
                 leaves nothing behind
      None     - return to the menu and run the setup of the scenario

    "run" is not automatic, and that is deliberate. A shared run also shares every change that
    the last scenario made to it. A scenario that depends on a clean run then fails for a
    reason that has nothing to do with the scenario. It can even pass for such a reason, which
    is worse. For example, the shop tests in one batch read the potions of each other. Use
    "run" only for a scenario that is fully independent.

    A batch also ignores the seed in the scenario, because the scenarios near it share one
    run. Thus a scenario that depends on a seed must not use a batch.
    """
    if "checks" not in scenario or scenario.get("batch") is False:
        return None
    setup = scenario.get("setup") or {}
    if not setup or set(setup) - {"character", "seed", "fight", "ascension"}:
        return None  # the scenario wants no run, or the setup is one that we do not know
    if scenario.get("batch") == "run" and "fight" not in setup:
        return "run"
    if setup.get("fight") == "SLIMES_WEAK":
        return "combat"
    return None


# Which test groups can a change to a given path break? The code matches the longest prefix
# first, so an entry for one file can make the entry for its directory more exact.
#
# This map makes the inner loop faster; it is not a full protection. A path with no match here
# runs the WHOLE suite, and `scripts/dev.sh release` always runs the whole suite. If you are
# not sure, make the map wider. A map that is too narrow lets a regression through. A map that
# is too wide costs only a few seconds.
_CHANGE_MAP: list[tuple[str, set[str]]] = [
    # Content: each type, and the pools and rendering that the compendium group asserts.
    ("AlchemistCode/Cards/", {"cards", "sweeps", "compendium"}),
    ("AlchemistCode/Powers/", {"cards", "sweeps", "compendium"}),
    ("AlchemistCode/Enchantments/", {"cards", "sweeps", "compendium"}),
    ("AlchemistCode/Commands/", {"cards", "sweeps"}),
    ("AlchemistCode/Relics/", {"sweeps", "rest", "shop", "compendium"}),
    ("AlchemistCode/Potions/", {"sweeps", "rest", "shop", "compendium"}),
    ("AlchemistCode/Epochs/", {"settings"}),
    ("AlchemistCode/Config/", {"settings"}),
    # Patches: these are narrow, because each patch hooks one subsystem.
    ("AlchemistCode/Patches/AncientDialoguePatches.cs", {"ancients"}),
    ("AlchemistCode/Patches/PotionSellPatches.cs", {"shop"}),
    ("AlchemistCode/Patches/RestSitePatches.cs", {"rest"}),
    ("AlchemistCode/Patches/EpochPatches.cs", {"settings"}),
    ("AlchemistCode/Patches/UnlockStateSerializationPatches.cs", {"settings"}),
    ("AlchemistCode/Patches/PoolPatches.cs", {"compendium"}),
    ("AlchemistCode/Patches/CardTextPatches.cs", {"cards", "compendium"}),
    ("AlchemistCode/Patches/KeywordTipPatches.cs", {"cards", "compendium"}),
    ("AlchemistCode/Patches/InfusionPatches.cs", {"cards", "sweeps"}),
    ("AlchemistCode/Patches/PoisonPatches.cs", {"cards", "sweeps"}),
    # Localization: loc_render (compendium) reads every file; the ancients group has its own file.
    ("Alchemist/localization/eng/ancients.json", {"ancients", "compendium"}),
    ("Alchemist/localization/", {"compendium"}),
    ("cards.csv", {"cards", "compendium"}),
    # These paths have no behaviour test: art, docs, and the release support files.
    ("Alchemist/images/", set()),
    ("images/", set()),
    ("docs/", set()),
    ("dist/", set()),
    (".github/", set()),
    ("README.md", set()),
    ("CHANGELOG.md", set()),
    ("CONTRIBUTING.md", set()),
    ("RELEASING.md", set()),
    ("BUILD.md", set()),
    ("CLAUDE.md", set()),
    ("LICENSE", set()),
]


def changed_files(ref: str | None) -> list[str]:
    """The repo-relative paths that differ from *ref* (default: uncommitted work against HEAD)."""
    cmds = ([["git", "diff", "--name-only", ref]] if ref
            else [["git", "diff", "--name-only", "HEAD"], ["git", "ls-files", "--others", "--exclude-standard"]])
    out: list[str] = []
    for cmd in cmds:
        r = subprocess.run(cmd, cwd=REPO_DIR, capture_output=True, text=True)
        if r.returncode != 0:
            raise CheckFailure(f"{' '.join(cmd)} failed: {r.stderr.strip()}")
        out += [ln for ln in r.stdout.splitlines() if ln.strip()]
    return sorted(set(out))


def groups_for_changes(paths: list[str]) -> tuple[set[str] | None, list[str]]:
    """Map the paths that changed to the test groups.

    It returns (groups, reasons). groups is None when a path that the map does not know
    changed, and thus the whole suite must run. An empty set means that no path with a
    test changed.
    """
    groups: set[str] = set()
    reasons: list[str] = []
    for path in paths:
        # A scenario that changed affects only its own group.
        if path.startswith("scripts/tests/") and path.endswith(".json"):
            grp = Path(path).parent.name
            groups.add(grp)
            reasons.append(f"{path} -> {grp}")
            continue
        # The test harness, the character, or a path with no entry in the map: run everything.
        match = next((m for m in sorted(_CHANGE_MAP, key=lambda kv: -len(kv[0]))
                      if path.startswith(m[0])), None)
        if match is None:
            reasons.append(f"{path} -> (no entry in the map, run everything)")
            return None, reasons
        if match[1]:
            groups |= match[1]
            reasons.append(f"{path} -> {', '.join(sorted(match[1]))}")
        else:
            reasons.append(f"{path} -> no tests")
    return groups, reasons


def discover(groups: set[str] | None, name_filters: list[str]) -> list[Path]:
    """The scenarios in *groups* (None means every group) that match one of *name_filters*."""
    paths = sorted(TESTS_DIR.rglob("*.json"))
    if groups is not None:
        paths = [p for p in paths if p.parent.name in groups]
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

    if "--game" in args:  # a mode for the dev.sh game-* commands that controls the process only
        action = take_opt("--game")
        ok = {"start": lambda: ensure_game_ready(),
              "stop": stop_game,
              "restart": lambda: ensure_game_ready(fresh=True)}[action]()
        print(("✓ " if ok else "✗ ") + f"game {action}")
        return 0 if ok else 1

    speed = float(take_opt("--speed") or os.environ.get("ALCH_TEST_SPEED", "3"))
    fast_mode = take_opt("--fast-mode") or os.environ.get("ALCH_TEST_FAST_MODE", "Instant")
    group = take_opt("--group")
    changed_ref = take_opt("--changed-since")
    changed = "--changed" in args
    if changed:
        args.remove("--changed")
    fresh = "--fresh" in args
    if fresh:
        args.remove("--fresh")
    name_filters = [a.lower() for a in args]

    groups: set[str] | None = {group} if group else None
    if changed or changed_ref:
        files = changed_files(changed_ref)
        if not files:
            print(f"✓ nothing changed against {changed_ref or 'HEAD'}; no scenario to run")
            return 0
        selected, reasons = groups_for_changes(files)
        print(f"── changed against {changed_ref or 'HEAD'} ({len(files)} file(s)) ──")
        for r in reasons:
            print(f"  {r}")
        if selected is None:
            print("  => run the full suite")
        elif not selected:
            print("  => no group is affected; nothing to run")
            return 0
        else:
            print(f"  => groups: {', '.join(sorted(selected))}")
            groups = selected if group is None else ({group} & selected)

    paths = discover(groups, name_filters)
    if not paths:
        print(f"✗ no scenarios match group={group!r} names={name_filters} in {TESTS_DIR}")
        return 2

    if not ensure_game_ready(fresh=fresh):
        print("✗ could not make the game ready; is Steam active? (scripts/dev.sh doctor)")
        return 2
    print("✓ the game is ready (the bridge and the explorer are connected)")

    no_batch = "--no-batch" in sys.argv
    apply_perf(speed, fast_mode)
    passed, failed, restarts = [], [], 0
    current_group = None
    in_batch: str | None = None  # the batch mode of the shared run that is open now, if one is open
    ctx_batch: dict = {}
    try:
        for path in paths:
            scenario = json.loads(path.read_text())
            grp = scenario.get("group") or path.parent.name
            if grp != current_group:
                current_group = grp
                print(f"\n── {grp} ──")
            desc = scenario.get("description", "")
            mode = None if no_batch else _batch_mode(scenario)

            # The scenarios in a batch share one run. This removes the return to the menu
            # and the start of the run (about 6.4 seconds for each scenario). Every other
            # scenario returns to the menu and runs with its own setup.
            skip_setup = False
            if mode is not None and mode == in_batch:
                if mode == "combat":
                    _fresh_fight(ctx_batch)  # a clean combat for the next scenario
                # "run": the scenario reaches its own room with the console, so there is nothing to reset
                skip_setup = True
            else:
                in_batch = None
                if not reset_to_menu():
                    restarts += 1
                    if restarts > 2 or not ensure_game_ready(fresh=True):
                        print(f"✗ {path.stem}: the game does not answer and the restart failed; stop here")
                        failed.append(path.stem)
                        break
                    apply_perf(speed, fast_mode)
                ctx_batch = {}
                if mode == "combat":
                    # this also opens the shared run, if no run is active, so its own setup is not needed
                    _fresh_fight(ctx_batch)
                    skip_setup = True
                # "run": let the first scenario start the run. That scenario needs a Neow
                # event, not the combat that _sweep_new_run makes. Also, `ancient X` does
                # not open from a combat.
                in_batch = mode

            t0 = time.monotonic()
            try:
                if "sweep" in scenario:
                    result = run_sweep(scenario["sweep"], scenario)
                    ok = result["passed"]
                    detail = [f"    {f}" for f in result["failures"]]
                elif "checks" in scenario:
                    result = run_checks_scenario(scenario, skip_setup=skip_setup)
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
            suffix = f": {desc}" if desc else ""
            print(f"{mark} {path.stem}  ({took:.1f}s){suffix}")
            (passed if ok else failed).append(path.stem)
            if not ok:
                for line in detail:
                    print(line)
    finally:
        try:  # return the game to its earlier condition: the menu, at normal speed
            reset_to_menu()
            bridge_client.set_game_speed(1.0)
        except Exception:
            pass

    print(f"\n{len(passed)} passed, {len(failed)} failed of {len(paths)} scenario(s)")
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
