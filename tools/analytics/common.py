"""Shared plumbing for the analytics scripts: reading the uploaded runs.

The mod posts each run to alchemist.fyi/api/runs (site/api/runs.ts), which queues it in Redis.
Once a day site/api/pack.ts moves the queue into one gzipped JSON Lines file in Vercel Blob and
keeps the list of files in Redis. So the runs are the files plus the queue. Both stores belong to
the Vercel project, which gives a build these variables:

    KV_REST_API_URL, KV_REST_API_READ_ONLY_TOKEN   the queue and the list of files
    VERCEL_OIDC_TOKEN                              the files (BLOB_READ_WRITE_TOKEN works too)

A local export reads them from the environment or from site/.env.local (gitignored). A file never
changes once written, so each is downloaded once and kept in site/node_modules/.cache, which
Vercel keeps between builds. Standard library only, so a build machine needs no pip install.
"""

import gzip
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
ENV_FILE = REPO / "site" / ".env.local"
CACHE = REPO / "site" / "node_modules" / ".cache" / "alchemist-runs"

INBOX = "runs:inbox"
FILES = "runs:files"

# Rows with this mod_version are fabricated by seed_runs.py and never counted
SEED_VERSION = "seed-test"

# Until the first daily pack, every run is still in Supabase, where older mod versions post.
# Delete this with the Supabase project
SUPABASE_RUNS = "https://qgvpsvjvgpfweeouufbk.supabase.co/rest/v1/runs"
SUPABASE_KEY_FILE = HERE / "supabase-service-key.local.txt"


def credential(name: str) -> str | None:
    if value := os.environ.get(name):
        return value
    if ENV_FILE.exists():
        for line in ENV_FILE.read_text(encoding="utf-8").splitlines():
            key, _, value = line.partition("=")
            if key.strip() == name:
                return value.strip().strip("\"'") or None
    return None


def missing_credentials_message(names: list[str]) -> str:
    return (f"Missing {', '.join(names)}. A Vercel build has them. For a local export, copy them from the "
            f"stores in the Vercel project's Storage tab into {ENV_FILE.relative_to(REPO)}, "
            "or use `npm run data:seed` for made-up runs.")


def add_common_args(parser) -> None:
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


def fetch_runs(mod_version: str | None = None, game_version: str | None = None,
               days_back: int | None = None) -> list[dict]:
    """Every run, oldest first."""
    queued, files = redis([["LRANGE", INBOX, 0, -1], ["GET", FILES]])
    files = json.loads(files) if files else []
    if files:
        lines = [line for file in files for line in blob_lines(file)] + queued
        # A pack that failed half way is packed again, so a run can be in two files
        rows = list({row["id"]: row for row in map(json.loads, lines)}.values())
    else:
        rows = supabase_rows() + [json.loads(line) for line in queued]

    since = (datetime.now(timezone.utc) - timedelta(days=days_back)).isoformat() if days_back else ""
    rows = [row for row in rows
            if (not mod_version or row["mod_version"] == mod_version)
            and (not game_version or row["game_version"] == game_version)
            and row["created_at"] >= since]
    return sorted(rows, key=lambda row: row["created_at"])


def redis(commands: list[list]) -> list:
    """The results of the commands, in order, from the Upstash REST API."""
    url, token = credential("KV_REST_API_URL"), credential("KV_REST_API_READ_ONLY_TOKEN")
    if not url or not token:
        raise SystemExit(missing_credentials_message(["KV_REST_API_URL", "KV_REST_API_READ_ONLY_TOKEN"]))
    request = urllib.request.Request(f"{url}/pipeline", data=json.dumps(commands).encode(), method="POST",
                                     headers={"Authorization": f"Bearer {token}"})
    answers = json.loads(read(request))
    if errors := [answer["error"] for answer in answers if "error" in answer]:
        raise SystemExit(f"Redis: {'; '.join(errors)}")
    return [answer["result"] for answer in answers]


def blob_lines(file: dict) -> list[str]:
    """The runs in one packed file, from the cache when it holds the whole file."""
    cached = CACHE / file["pathname"]
    if not cached.exists() or cached.stat().st_size != file["size"]:
        token = credential("VERCEL_OIDC_TOKEN") or credential("BLOB_READ_WRITE_TOKEN")
        if not token:
            raise SystemExit(missing_credentials_message(["VERCEL_OIDC_TOKEN or BLOB_READ_WRITE_TOKEN"]))
        body = read(urllib.request.Request(file["url"], headers={"Authorization": f"Bearer {token}"}))
        cached.parent.mkdir(parents=True, exist_ok=True)
        cached.write_bytes(body)
    return gzip.decompress(cached.read_bytes()).decode("utf-8").splitlines()


def supabase_rows() -> list[dict]:
    key = os.environ.get("SUPABASE_READ_KEY")
    if not key and SUPABASE_KEY_FILE.exists():
        key = SUPABASE_KEY_FILE.read_text(encoding="utf-8").strip()
    if not key:
        return []
    columns = "created_at,mod_version,game_version,victory,ascension,floor,playtime,player_hash,epochs,data,alchemist"
    url = f"{SUPABASE_RUNS}?{urllib.parse.urlencode({'select': columns, 'order': 'id.asc'})}"
    page, rows = 1000, []
    for start in range(0, 1_000_000, page):
        request = urllib.request.Request(url, headers={
            "apikey": key, "Authorization": f"Bearer {key}", "Range": f"{start}-{start + page - 1}"})
        batch = json.loads(read(request))
        rows.extend(batch)
        if len(batch) < page:
            return rows
    return rows


def read(request: urllib.request.Request, attempts: int = 3) -> bytes:
    """The response body. A slow, dropped or cut-off read and a busy server (429 or 5xx) are tried
    again, so one hiccup does not fail a daily build; any other HTTP error gets the same answer twice."""
    context = ssl_context()
    for attempt in range(1, attempts + 1):
        try:
            with urllib.request.urlopen(request, timeout=60, context=context) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            if error.code != 429 and error.code < 500 or attempt == attempts:
                raise
        except (urllib.error.URLError, http.client.HTTPException, ssl.SSLError, TimeoutError, ConnectionError):
            if attempt == attempts:
                raise
        time.sleep(5 * attempt)
    return b""
