#!/usr/bin/env python3
"""Exercise the real metadata scanner using disposable asset fixtures."""
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parent.parent
SCRIPT = ROOT / "Tools/validate-meta-guids.py"
spec = importlib.util.spec_from_file_location("meta_audit", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
GUID = "1234567890abcdef1234567890abcdef"
OTHER = "abcdef1234567890abcdef1234567890"


def fixture(root):
    assets = root / "Assets"
    assets.mkdir()
    (assets / "sample.txt").write_text("fixture")
    (assets / "sample.txt.meta").write_text("fileFormatVersion: 2\nguid: " + GUID + "\n")
    return assets


def run(name, change, expected):
    with tempfile.TemporaryDirectory(prefix="emberfall-meta-test-") as tmp:
        root = Path(tmp)
        assets = fixture(root)
        change(assets)
        before = {p.relative_to(root): p.read_bytes() for p in root.rglob("*") if p.is_file()}
        report = module.audit(root)
        kinds = {x["kind"] for x in report["failures"]}
        assert kinds == set(expected), (name, report)
        assert report["passed"] == (not expected), (name, report)
        assert report == module.audit(root), "diagnostics must be deterministic"
        after = {p.relative_to(root): p.read_bytes() for p in root.rglob("*") if p.is_file()}
        assert before == after, "scanner changed fixture"
        result = subprocess.run([sys.executable, str(SCRIPT), "--root", str(root)], capture_output=True)
        assert result.returncode == (1 if expected else 0), (name, result.stderr)
        print("PASS metadata audit fixture:", name)


run("valid", lambda a: None, [])
for name, value in [("short", GUID[:-1]), ("long", GUID + "a"), ("nonhex", "x" * 32),
                    ("empty", ""), ("zero", "0" * 32)]:
    run(name, lambda a, v=value: (a / "sample.txt.meta").write_text("guid: " + v + "\n"), ["invalid_guid"])
run("missing declaration", lambda a: (a / "sample.txt.meta").write_text("fileFormatVersion: 2\n"), ["guid_declaration_count"])
run("multiple declarations", lambda a: (a / "sample.txt.meta").write_text("guid: " + GUID + "\nguid: " + OTHER + "\n"), ["guid_declaration_count"])
run("nested reference/BOM/CRLF", lambda a: (a / "sample.txt.meta").write_bytes(("\ufeffguid: " + GUID + "\r\nImporter:\r\n  externalObjects:\r\n    guid: " + OTHER + "\r\n").encode()), [])
run("missing file meta", lambda a: (a / "sample.txt.meta").unlink(), ["missing_meta"])
run("missing folder meta", lambda a: (a / "folder").mkdir(), ["missing_meta"])
run("orphan file meta", lambda a: (a / "sample.txt").unlink(), ["orphan_meta"])
run("folder/file mismatch", lambda a: (a / "sample.txt.meta").write_text("guid: " + GUID + "\nfolderAsset: yes\n"), ["asset_kind_mismatch"])
run("invalid folder marker", lambda a: (a / "sample.txt.meta").write_text("guid: " + GUID + "\nfolderAsset: perhaps\n"), ["invalid_folder_marker"])
run("empty folder preserved", lambda a: (a / "folder.meta").write_text("guid: " + OTHER + "\nfolderAsset: yes \n"), [])
run("case-insensitive file/folder collision", lambda a: (a / "folder.meta").write_text("guid: " + GUID.upper() + "\nfolderAsset: yes\n"), ["duplicate_guid"])
run("hidden assets included", lambda a: (a / ".hidden").write_text("asset"), ["missing_meta"])
run("symlink rejected", lambda a: (a / "link").symlink_to(a / "sample.txt"), ["symlink"])
print("PASS metadata scanner controls: 18 fixtures; all intended failures detected")
