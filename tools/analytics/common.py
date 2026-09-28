"""Shared plumbing for the analytics scripts: the Supabase fetch and the read key.

Reads need the secret key (sb_secret_..., or the legacy service_role key). Never commit it. Put
it in tools/analytics/supabase-service-key.local.txt (gitignored) or the SUPABASE_READ_KEY env
var. The publishable key in the DLL can only insert, so it cannot read anything.
"""

import http.client
import json
import os
import ssl
import time
import urllib.error
import urllib.parse
import urllib.request
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


def ssl_context() -> ssl.SSLContext:
    """The system's certificates, or certifi's where Python has none (the python.org macOS build)."""
    try:
        import certifi
        return ssl.create_default_context(cafile=certifi.where())
    except ImportError:
        return ssl.create_default_context()


def fetch_runs(key: str, mod_version: str | None = None, game_version: str | None = None,
               days_back: int | None = None) -> list[dict]:
    """Every run row, oldest first. PostgREST pages at 1000 rows by default so this walks the ranges.
    Standard library only, so a build machine needs no pip install."""
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

    url = f"{RUNS_URL}?{urllib.parse.urlencode(params)}"
    headers = {"apikey": key, "Authorization": f"Bearer {key}"}
    page, rows, context = 1000, [], ssl_context()
    for start in range(0, 1_000_000, page):
        request = urllib.request.Request(url, headers=headers | {"Range": f"{start}-{start + page - 1}"})
        batch = read_page(request, context)
        rows.extend(batch)
        if len(batch) < page:
            break
    return rows


def read_page(request: urllib.request.Request, context: ssl.SSLContext, attempts: int = 3) -> list[dict]:
    """One page of rows. A slow, dropped or cut-off read and a busy server (429 or 5xx) are tried
    again, so one hiccup does not fail a daily build; any other HTTP error gets the same answer twice."""
    for attempt in range(1, attempts + 1):
        try:
            with urllib.request.urlopen(request, timeout=60, context=context) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            if error.code != 429 and error.code < 500 or attempt == attempts:
                raise
        except (urllib.error.URLError, http.client.HTTPException, ssl.SSLError, TimeoutError, ConnectionError):
            if attempt == attempts:
                raise
        time.sleep(5 * attempt)
    return []
