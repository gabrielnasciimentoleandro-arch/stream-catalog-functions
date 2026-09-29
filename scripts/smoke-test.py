#!/usr/bin/env python3
"""Run end-to-end HTTP checks against a running Stream Catalog host."""

from __future__ import annotations

import concurrent.futures
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE_URL = os.getenv("STREAM_CATALOG_BASE_URL", "http://127.0.0.1:7071").rstrip("/")
FUNCTION_KEY = os.getenv("STREAM_CATALOG_FUNCTION_KEY")
checks: list[str] = []


def request(
    method: str,
    path: str,
    body: bytes | str | None = None,
    content_type: str | None = None,
) -> tuple[int, dict[str, str], bytes]:
    data = body.encode() if isinstance(body, str) else body
    headers: dict[str, str] = {}
    if content_type:
        headers["Content-Type"] = content_type
    if FUNCTION_KEY and method in {"POST", "PUT", "PATCH", "DELETE"}:
        headers["x-functions-key"] = FUNCTION_KEY

    request_message = urllib.request.Request(
        BASE_URL + path,
        data=data,
        headers=headers,
        method=method,
    )
    try:
        with urllib.request.urlopen(request_message, timeout=30) as response:
            return response.status, dict(response.headers), response.read()
    except urllib.error.HTTPError as error:
        return error.code, dict(error.headers), error.read()


def decode_json(body: bytes) -> object:
    try:
        return json.loads(body)
    except (json.JSONDecodeError, UnicodeDecodeError) as error:
        raise AssertionError(f"Expected JSON, received {body[:500]!r}") from error


def check(name: str, condition: bool, detail: object = "") -> None:
    if not condition:
        raise AssertionError(f"{name}: {detail}")
    checks.append(name)
    print(f"PASS {len(checks):02d}: {name}")


def main() -> int:
    status, headers, body = request("GET", "/api/v1")
    metadata = decode_json(body)
    check(
        "service metadata and security headers",
        status == 200
        and isinstance(metadata, dict)
        and metadata.get("name") == "Stream Catalog Functions"
        and headers.get("X-Content-Type-Options") == "nosniff"
        and "default-src" in headers.get("Content-Security-Policy", ""),
    )

    status, _, body = request("GET", "/api/v1/health")
    health = decode_json(body)
    check(
        "health and configured providers",
        status == 200
        and isinstance(health, dict)
        and health.get("status") == "Healthy",
        health,
    )

    status, headers, body = request("GET", "/api/v1/openapi")
    check(
        "embedded OpenAPI contract",
        status == 200
        and b"openapi: 3.0.3" in body
        and headers.get("Content-Type", "").startswith("application/yaml"),
    )

    status, _, body = request("GET", "/api/v1/catalog")
    page = decode_json(body)
    check(
        "catalog listing",
        status == 200
        and isinstance(page, dict)
        and isinstance(page.get("items"), list),
        page,
    )

    status, _, body = request("GET", "/api/v1/catalog/search")
    problem = decode_json(body)
    check(
        "search requires a filter",
        status == 400
        and isinstance(problem, dict)
        and problem.get("code") == "validation_failed",
        problem,
    )

    image = b"\x89PNG\r\n\x1a\nstream-catalog-smoke-test"
    status, headers, body = request(
        "POST",
        "/api/v1/covers?fileName=arrival.png",
        image,
        "image/png",
    )
    upload = decode_json(body)
    check(
        "cover upload",
        status == 201
        and isinstance(upload, dict)
        and str(upload.get("fileName", "")).endswith(".png")
        and upload.get("length") == len(image),
        upload,
    )
    cover_name = str(upload["fileName"])
    check("cover upload location", headers.get("Location") == upload.get("downloadUrl"))

    status, headers, body = request("GET", f"/api/v1/covers/{cover_name}")
    check(
        "cover download",
        status == 200 and body == image and headers.get("Content-Type") == "image/png",
    )
    check("immutable cover cache", "immutable" in headers.get("Cache-Control", ""))

    payload = {
        "title": "Arrival",
        "synopsis": "A linguist works with visitors from another world.",
        "type": "movie",
        "genres": ["Science Fiction", "Drama"],
        "releaseYear": 2016,
        "ageRating": "12",
        "coverFileName": cover_name,
    }
    status, headers, body = request(
        "POST",
        "/api/v1/catalog",
        json.dumps(payload),
        "application/json",
    )
    created = decode_json(body)
    check(
        "catalog item creation",
        status == 201
        and isinstance(created, dict)
        and created.get("title") == "Arrival"
        and len(str(created.get("id", ""))) == 32,
        created,
    )
    item_id = str(created["id"])
    check(
        "catalog item location",
        headers.get("Location") == f"/api/v1/catalog/items/{item_id}",
    )

    status, _, body = request("GET", f"/api/v1/catalog/items/{item_id}")
    detail = decode_json(body)
    check(
        "catalog item detail",
        status == 200 and isinstance(detail, dict) and detail.get("id") == item_id,
        detail,
    )

    status, _, body = request("GET", "/api/v1/catalog?page=1&pageSize=100")
    page = decode_json(body)
    check(
        "catalog pagination",
        status == 200
        and isinstance(page, dict)
        and any(item.get("id") == item_id for item in page.get("items", [])),
        page,
    )

    query = urllib.parse.urlencode(
        {
            "title": "rri",
            "genre": "Science Fiction",
            "type": "movie",
            "releaseYear": "2016",
            "pageSize": "100",
        }
    )
    status, _, body = request("GET", f"/api/v1/catalog/search?{query}")
    filtered = decode_json(body)
    check(
        "catalog combined filters",
        status == 200
        and isinstance(filtered, dict)
        and any(item.get("id") == item_id for item in filtered.get("items", [])),
        filtered,
    )

    status, headers, body = request(
        "POST", "/api/v1/catalog", '{"type":0}', "application/json"
    )
    problem = decode_json(body)
    check(
        "numeric enum rejected",
        status == 400
        and isinstance(problem, dict)
        and problem.get("code") == "invalid_json"
        and headers.get("Content-Type", "").startswith("application/problem+json"),
        problem,
    )

    status, _, body = request(
        "POST", "/api/v1/catalog", '{"unknown":true}', "application/json"
    )
    problem = decode_json(body)
    check(
        "unknown JSON property rejected",
        status == 400
        and isinstance(problem, dict)
        and problem.get("code") == "invalid_json",
        problem,
    )

    status, _, body = request("POST", "/api/v1/catalog", "{", "application/json")
    problem = decode_json(body)
    check(
        "malformed JSON rejected",
        status == 400
        and isinstance(problem, dict)
        and problem.get("code") == "invalid_json",
        problem,
    )

    status, _, body = request(
        "POST", "/api/v1/covers?fileName=poster.gif", b"gif", "image/gif"
    )
    problem = decode_json(body)
    check(
        "unsupported image rejected",
        status == 415
        and isinstance(problem, dict)
        and problem.get("code") == "unsupported_media_type",
        problem,
    )

    status, _, body = request(
        "POST", "/api/v1/covers?fileName=poster.jpg", b"", "image/jpeg"
    )
    problem = decode_json(body)
    check(
        "empty image rejected",
        status == 400
        and isinstance(problem, dict)
        and problem.get("code") == "validation_failed",
        problem,
    )

    status, _, body = request("GET", "/api/v1/catalog/items/" + ("f" * 32))
    problem = decode_json(body)
    check(
        "missing catalog item",
        status == 404
        and isinstance(problem, dict)
        and problem.get("code") == "not_found",
        problem,
    )

    status, _, body = request("GET", "/api/v1/covers/" + ("f" * 32) + ".jpg")
    problem = decode_json(body)
    check(
        "missing cover",
        status == 404
        and isinstance(problem, dict)
        and problem.get("code") == "not_found",
        problem,
    )

    with concurrent.futures.ThreadPoolExecutor(max_workers=10) as pool:
        statuses = list(
            pool.map(lambda _: request("GET", "/api/v1/health")[0], range(30))
        )
    check("30 concurrent health requests", statuses == [200] * 30, statuses)

    large_image = b"x" * (5 * 1024 * 1024 + 1)
    status, _, body = request(
        "POST",
        "/api/v1/covers?fileName=large.jpg",
        large_image,
        "image/jpeg",
    )
    problem = decode_json(body)
    check(
        "5 MiB upload limit",
        status == 413
        and isinstance(problem, dict)
        and problem.get("code") == "payload_too_large",
        problem,
    )

    print(f"\nSMOKE RESULT: {len(checks)}/{len(checks)} groups passed")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (AssertionError, OSError) as error:
        print(f"SMOKE FAILED: {error}", file=sys.stderr)
        sys.exit(1)
