#!/usr/bin/env python3
"""Package the AeroDesk installer and documentation into a Windows-mountable ISO."""
from __future__ import annotations
import argparse
from pathlib import Path
import sys

try:
    import pycdlib
except ImportError as exc:
    raise SystemExit("Missing dependency. Install it with: python -m pip install pycdlib") from exc


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path, help="Folder containing AeroDesk-Setup.exe and README.txt")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    source = args.input.resolve()
    installer = source / "AeroDesk-Setup.exe"
    readme = source / "README.txt"
    if not installer.is_file():
        raise SystemExit(f"Installer not found: {installer}")
    if not readme.is_file():
        raise SystemExit(f"README not found: {readme}")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    iso = pycdlib.PyCdlib()
    iso.new(interchange_level=3, joliet=3, vol_ident="AERODESK")
    iso.add_file(str(installer), iso_path="/AERODSK.EXE;1", joliet_path="/AeroDesk-Setup.exe")
    iso.add_file(str(readme), iso_path="/README.TXT;1", joliet_path="/README.txt")
    iso.write(str(args.output))
    iso.close()
    print(f"ISO created: {args.output} ({args.output.stat().st_size:,} bytes)")
    return 0

if __name__ == "__main__":
    sys.exit(main())
