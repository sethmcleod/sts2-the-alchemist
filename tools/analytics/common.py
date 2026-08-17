"""Shared plumbing for the analytics scripts: the Supabase fetch and the read-key discovery.

The card metadata comes from card_meta.py. The interactive dashboard is docs/analytics/index.html,
fed by the aggregate JSON that export_stats.py writes to docs/analytics/data/.

Read access needs the secret key (sb_secret_..., or the legacy service_role key). It is never committed: put it in
tools/analytics/supabase-service-key.local.txt (gitignored) or the SUPABASE_READ_KEY env var.
The publishable key baked into the DLL is insert-only and cannot read anything.
"""

import os
from datetime import datetime, timedelta, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]

# The Supabase project. Override with SUPABASE_URL for a second project (a staging one, say)
SUPABASE_URL = os.environ.get("SUPABASE_URL", "https://qgvpsvjvgpfweeouufbk.supabase.co")
RUNS_URL = f"{SUPABASE_URL}/rest/v1/runs" if SUPABASE_URL else ""

KEY_FILE = HERE / "supabase-service-key.local.txt"

# Rows with this mod_version are fabricated by seed_runs.py and never counted
SEED_VERSION = "seed-test"


def default_key() -> str | None:
    if key := os.environ.get("SUPABASE_READ_KEY"):
        return key
    if KEY_FILE.exists():
        return KEY_FILE.read_text(encoding="utf-8").strip()
    return None


def missing_key_message() -> str:
    return ("No read key: pass --key, set SUPABASE_READ_KEY, or write the secret key to "
            f"{KEY_FILE.relative_to(REPO)} (the publishable key is insert-only and will not work).")


def missing_url_message() -> str:
    return "No project URL: set SUPABASE_URL to https://<project>.supabase.co"


def add_common_args(parser) -> None:
    parser.add_argument("--key", default=default_key())
    parser.add_argument("--mod-version", default=None, help="one mod release (default: all)")
    parser.add_argument("--game-version", default=None, help="one game build (default: all)")
    parser.add_argument("--days-back", type=int, default=None, help="only runs from the last N days")


def fetch_runs(key: str, mod_version: str | None = None, game_version: str | None = None,
               days_back: int | None = None) -> list[dict]:
    """Every run row, oldest first. PostgREST pages at 1000 rows by default so this walks the ranges."""
    import requests  # only the network paths need it, so the offline ones do not
    if not RUNS_URL:
        raise SystemExit(missing_url_message())
    params = {
        "select": "created_at,mod_version,game_version,victory,ascension,floor,playtime,"
                  "player_hash,epochs,data,alchemist",
        "order": "created_at.asc",
    }
    if mod_version:
        params["mod_version"] = f"eq.{mod_version}"
    if game_version:
        params["game_version"] = f"eq.{game_version}"
    if days_back:
        since = datetime.now(timezone.utc) - timedelta(days=days_back)
        params["created_at"] = f"gte.{since.isoformat()}"

    headers = {"apikey": key, "Authorization": f"Bearer {key}"}
    page, rows = 1000, []
    for start in range(0, 1_000_000, page):
        resp = requests.get(RUNS_URL, params=params, timeout=60,
                            headers=headers | {"Range": f"{start}-{start + page - 1}"})
        resp.raise_for_status()
        batch = resp.json()
        rows.extend(batch)
        if len(batch) < page:
            break
    return rows
