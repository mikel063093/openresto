#!/usr/bin/env python3
"""Create context-rich candidate-copy inventories for translation review.

The generated files are review inputs, not runtime catalogs. They deliberately keep
file path, line number, and source snippet so translators can distinguish UI copy
from technical strings before a catalog key is assigned.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "i18n" / "inventory"

AREAS = {
    "public-ui": [ROOT / "openresto-frontend" / "app" / "(user)", ROOT / "openresto-frontend" / "components" / "booking", ROOT / "openresto-frontend" / "components" / "restaurant", ROOT / "openresto-frontend" / "components" / "layout", ROOT / "openresto-frontend" / "components" / "common"],
    "admin-ui": [ROOT / "openresto-frontend" / "app" / "admin", ROOT / "openresto-frontend" / "components" / "admin"],
    "backend-api": [ROOT / "OpenRestoApi"],
}

ATTRIBUTES = re.compile(r'(?:accessibilityLabel|accessibilityHint|placeholder|title|label|message|headerTitle)\s*=\s*["`]([^"`\n]{2,})["`]')
JSX_TEXT = re.compile(r">\s*([A-Za-z][^<{\n]{1,160}?)\s*<")
CS_MESSAGE = re.compile(r'(?:Message|message|error|Error)\s*=\s*"([^"\n]{2,})"')


def add(records: list[dict], seen: set[tuple], file: Path, line: int, kind: str, text: str, source: str) -> None:
    value = re.sub(r"\s+", " ", text).strip()
    if len(value) < 2 or not re.search(r"[A-Za-z]", value):
        return
    key = (str(file.relative_to(ROOT)), line, kind, value)
    if key in seen:
        return
    seen.add(key)
    records.append({
        "id": None,
        "sourceLocale": "en",
        "targetLocale": "es-CO",
        "status": "needs-review",
        "file": str(file.relative_to(ROOT)),
        "line": line,
        "kind": kind,
        "source": value,
        "translation": "",
        "context": source.strip()[:240],
    })


def scan_file(file: Path) -> list[dict]:
    text = file.read_text(encoding="utf-8")
    records: list[dict] = []
    seen: set[tuple] = set()
    for line_number, line in enumerate(text.splitlines(), start=1):
        if line.lstrip().startswith(("//", "*", "#")):
            continue
        for match in ATTRIBUTES.finditer(line):
            add(records, seen, file, line_number, "accessibility-or-field-label", match.group(1), line)
        if file.suffix == ".tsx":
            for match in JSX_TEXT.finditer(line):
                add(records, seen, file, line_number, "jsx-text", match.group(1), line)
        if file.suffix == ".cs":
            for match in CS_MESSAGE.finditer(line):
                add(records, seen, file, line_number, "api-message", match.group(1), line)
    return records


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = {}
    for batch, roots in AREAS.items():
        suffix = ".cs" if batch == "backend-api" else ".tsx"
        records = []
        for directory in roots:
            if directory.exists():
                for file in sorted(directory.rglob(f"*{suffix}")):
                    if "tests" not in file.parts:
                        records.extend(scan_file(file))
        records.sort(key=lambda row: (row["file"], row["line"], row["source"]))
        for number, row in enumerate(records, start=1):
            row["id"] = f"{batch}-{number:04d}"
        output = OUT / f"{batch}.json"
        output.write_text(json.dumps({"batch": batch, "sourceLocale": "en", "targetLocale": "es-CO", "entries": records}, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        manifest[batch] = {"file": str(output.relative_to(ROOT)), "entries": len(records)}
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
