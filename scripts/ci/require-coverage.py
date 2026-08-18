#!/usr/bin/env python3
"""Fail a CI job unless a coverage report meets a total line threshold."""
from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def fail(message: str) -> None:
    print(f"coverage gate failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def parse_cobertura(path: Path) -> float:
    root = ET.parse(path).getroot()
    value = root.attrib.get("line-rate")
    if value is None:
        fail(f"{path} has no Cobertura line-rate")
        raise AssertionError("unreachable")
    return float(value) * 100


def parse_jest(path: Path) -> float:
    report = json.loads(path.read_text(encoding="utf-8"))
    total = report.get("total", {}).get("lines", {})
    covered = total.get("covered")
    found = total.get("total")
    if not isinstance(covered, int) or not isinstance(found, int) or found <= 0:
        fail(f"{path} has no usable Jest total line coverage")
    return covered * 100 / found


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--format", choices=("cobertura", "jest"), required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--minimum", type=float, default=80.0)
    args = parser.parse_args()
    if not args.report.is_file():
        fail(f"report does not exist: {args.report}")
    percent = parse_cobertura(args.report) if args.format == "cobertura" else parse_jest(args.report)
    print(f"total line coverage: {percent:.2f}% (required: {args.minimum:.2f}%)")
    if percent < args.minimum:
        fail(f"{percent:.2f}% is below {args.minimum:.2f}%")


if __name__ == "__main__":
    main()
