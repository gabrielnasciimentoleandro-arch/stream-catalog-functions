#!/usr/bin/env python3
"""Fail when a Cobertura report is below the configured thresholds."""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("report", type=Path)
    parser.add_argument("--minimum-line", type=float, default=90.0)
    parser.add_argument("--minimum-branch", type=float, default=90.0)
    return parser.parse_args()


def main() -> int:
    arguments = parse_arguments()
    root = ET.parse(arguments.report).getroot()
    line_rate = float(root.attrib["line-rate"]) * 100
    branch_rate = float(root.attrib["branch-rate"]) * 100
    print(f"Line coverage: {line_rate:.2f}%")
    print(f"Branch coverage: {branch_rate:.2f}%")

    if line_rate < arguments.minimum_line or branch_rate < arguments.minimum_branch:
        print("Coverage threshold not met.", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
