#!/usr/bin/env python3
"""Read-only authored Assets metadata audit; works in a checkout or source ZIP.

Checks every file/folder under Assets (including hidden entries; no extension
allowlist), except Assets itself. Does not import assets or resolve package,
builtin, subasset/fileID or runtime references. Empty folder metas are accepted
because Unity recreates their untracked empty directory on import.
"""
import argparse
from collections import defaultdict
import json
from pathlib import Path
import re
import sys


def audit(root):
    root = Path(root).resolve()
    assets = root / "Assets"
    failures = []
    identities = defaultdict(list)
    empty_folders = []

    def fail(kind, path, **detail):
        failures.append(dict(kind=kind, path=path.relative_to(root).as_posix(), **detail))

    if not assets.is_dir() or assets.is_symlink():
        fail("missing_or_symlink_assets_root", assets)
        return {"passed": False, "failures": failures}
    paths = sorted(assets.rglob("*"))
    metas = [p for p in paths if p.name.endswith(".meta")]
    for path in paths:
        if path.is_symlink():
            fail("symlink", path)
        elif not path.is_file() and not path.is_dir():
            fail("nonregular_entry", path)
        elif not path.name.endswith(".meta"):
            meta = path.with_name(path.name + ".meta")
            if not meta.is_file() or meta.is_symlink():
                fail("missing_meta", path)
    for meta in metas:
        if not meta.is_file() or meta.is_symlink():
            fail("nonregular_meta", meta)
            continue
        try:
            content = meta.read_text(encoding="utf-8-sig")
        except UnicodeError:
            fail("invalid_meta_encoding", meta)
            continue
        # Only the root declaration owns identity; nested guid keys are references.
        values = re.findall(r"^guid\s*:[ \t]*([^\r\n]*)$", content, re.MULTILINE)
        if len(values) != 1:
            fail("guid_declaration_count", meta, count=len(values))
        else:
            value = values[0].strip()
            if not re.fullmatch(r"[0-9a-fA-F]{32}", value) or int(value, 16) == 0:
                fail("invalid_guid", meta, value=value)
            else:
                identities[value.lower()].append(meta.relative_to(root).as_posix())
        folder_values = [v.strip() for v in re.findall(r"^folderAsset\s*:[ \t]*([^\r\n]*)$", content, re.MULTILINE)]
        if len(folder_values) > 1 or (folder_values and folder_values[0].strip() not in ("yes", "no")):
            fail("invalid_folder_marker", meta)
        folder = folder_values == ["yes"]
        target = meta.with_name(meta.name[:-5])
        if target.exists():
            if target.is_dir() != folder:
                fail("asset_kind_mismatch", meta)
        elif folder:
            # Git does not store empty directories; preserve valid folder identity.
            empty_folders.append(target.relative_to(root).as_posix())
        else:
            fail("orphan_meta", meta)
    for guid, owners in sorted(identities.items()):
        if len(owners) > 1:
            failures.append(dict(kind="duplicate_guid", guid=guid, paths=owners))
    return {
        "passed": not failures,
        "scope": "All authored Assets files/folders and meta identities; no Unity import or external reference resolution",
        "assetFiles": sum(p.is_file() and not p.name.endswith(".meta") for p in paths),
        "assetFolders": sum(p.is_dir() for p in paths),
        "metaFiles": len(metas),
        "validUniqueGuidCount": len(identities),
        "emptyFolderMetas": empty_folders,
        "failures": failures,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--output", type=Path, help="Optional report outside Assets; source is never edited")
    args = parser.parse_args()
    if args.output and args.output.resolve().is_relative_to((args.root / "Assets").resolve()):
        parser.error("Report output must be outside Assets")
    result = audit(args.root)
    text = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(text, encoding="utf-8")
    print(text, end="")
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
