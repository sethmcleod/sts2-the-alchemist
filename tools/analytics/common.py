"""Shared plumbing for the analytics scripts: reading the uploaded runs"""

import gzip
import http.client
import json
import os
import ssl
import time
import urllib.error
import urllib.request
from pathlib import Path


HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
ENV_FILE = REPO / "site" / ".env.local"
CACHE = REPO / "site" / "node_modules" / ".cache" / "alchemist-runs"

INBOX = "runs:inbox"
FILES = "runs:files"

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
            f"stores in the Vercel project's Storage tab into {ENV_FILE.relative_to(REPO)}.")


def ssl_context() -> ssl.SSLContext:
    try:
        import certifi
        return ssl.create_default_context(cafile=certifi.where())
    except ImportError:
        return ssl.create_default_context()


def fetch_runs() -> list[dict]:
    queued, files = redis([["LRANGE", INBOX, 0, -1], ["GET", FILES]])
    files = json.loads(files) if files else []
    lines = [line for file in files for line in blob_lines(file)] + queued
    rows = list({row["id"]: row for row in map(json.loads, lines)}.values())
    return sorted(rows, key=lambda row: row["created_at"])


def redis(commands: list[list]) -> list:
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
    cached = CACHE / file["pathname"]
    if not cached.exists() or cached.stat().st_size != file["size"]:
        token = credential("VERCEL_OIDC_TOKEN") or credential("BLOB_READ_WRITE_TOKEN")
        if not token:
            raise SystemExit(missing_credentials_message(["VERCEL_OIDC_TOKEN or BLOB_READ_WRITE_TOKEN"]))
        body = read(urllib.request.Request(file["url"], headers={"Authorization": f"Bearer {token}"}))
        cached.parent.mkdir(parents=True, exist_ok=True)
        cached.write_bytes(body)
    return gzip.decompress(cached.read_bytes()).decode("utf-8").splitlines()


def read(request: urllib.request.Request, attempts: int = 3) -> bytes:
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
