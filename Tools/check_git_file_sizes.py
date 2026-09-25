#!/usr/bin/env python3
"""Check actual Git blobs in the index or a revision for GitHub's 100 MiB limit."""

import argparse
import subprocess
import sys


LIMIT = 100 * 1024 * 1024


def git(*args, data=None):
    return subprocess.check_output(["git", *args], input=data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--revision", help="Check this revision and its history instead of the index."
    )
    args = parser.parse_args()
    objects = {}
    if args.revision:
        for line in git("rev-list", "--objects", args.revision, "--").splitlines():
            oid, _, path = line.partition(b" ")
            objects[oid] = path.decode("utf-8", errors="replace")
    else:
        for entry in git("ls-files", "--stage", "-z").split(b"\0"):
            if not entry:
                continue
            metadata, path = entry.split(b"\t", 1)
            mode, oid, stage = metadata.split()
            if stage != b"0":
                print("FAIL: resolve merge conflicts before checking sizes.")
                return 1
            if mode != b"160000":  # Submodule objects live in a different repository.
                objects[oid] = path.decode("utf-8", errors="replace")
    if not objects:
        print("PASS: no Git objects to check.")
        return 0
    result = git(
        "cat-file", "--batch-check=%(objectname) %(objecttype) %(objectsize)",
        data=b"\n".join(objects) + b"\n",
    )
    oversized = []
    for line in result.splitlines():
        fields = line.split()
        if len(fields) != 3:
            raise RuntimeError("Cannot inspect Git object: " + line.decode())
        oid, kind, size = fields
        if kind == b"blob" and int(size) > LIMIT:
            oversized.append((int(size), objects[oid]))
    for size, path in sorted(oversized, reverse=True):
        print(f"FAIL: {size / 1024**2:.2f} MiB  {path}")
    if oversized:
        print("Use Git LFS and re-add these files; existing commits need separate migration.")
        return 1
    scope = args.revision + " history" if args.revision else "index (untracked/unstaged contents excluded)"
    print(f"PASS: {scope}; no regular Git blob exceeds 100 MiB.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (subprocess.CalledProcessError, RuntimeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        sys.exit(2)
