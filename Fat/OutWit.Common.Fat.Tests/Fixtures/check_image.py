#!/usr/bin/env python3
"""Judges a FAT or exFAT image written by OutWit.Common.Fat.

Runs inside WSL (Ubuntu) or another Linux, with the tools make_fixtures.py
needs. The tests call it with an image and a JSON file of what the library
believes the volume holds:

    {"kind": "fat16", "sectorSize": 512, "partitionStart": 0,
     "entries": [{"path": "/a.txt", "type": "file", "size": 3, "sha256": "...",
                  "shortName": "A.TXT", "clusters": [[5, 5]]}, ...]}

and on exFAT ("kind": "exfat") the entries may carry "contiguous" and
"validDataLength" instead of "shortName". It checks, with tools that share no
code with the library:

- fsck is clean. fsck.vfat: both FAT copies agree, no lost or cross-linked
  clusters, sizes match chains, FSInfo's free count is right. fsck.exfat: entry
  set checksums and name hashes, the bitmap against the chains, sizes against
  clusters, the up-case table;
- the kernel lists exactly those files and directories, with those sizes and
  hashes;
- on FAT, mdir shows those 8.3 aliases, mshowfat those cluster runs and mattrib
  those attributes, for names inside the BMP: mtools cannot address the others;
- on exFAT, dump.exfat shows those cluster runs, contiguity, valid data lengths
  and attributes;
- the volume has that geometry, serial number, label (on FAT in the root and in
  the boot sector) and number of free clusters.

It prints {"problems": [...], "fsck": "..."} and exits 0 when there are none.
With --fsck it runs fsck alone, for images the tests damaged on purpose; only
"kind", "sectorSize" and "partitionStart" are read from the JSON file then.

    python3 check_image.py IMAGE EXPECTED.json
    python3 check_image.py --fsck IMAGE EXPECTED.json
    python3 check_image.py --probe            # exits 0 when everything it needs is here
"""

import json
import os
import re
import shutil
import sys
import tempfile
from types import SimpleNamespace

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from fixture_content import read_back  # noqa: E402
from fixture_oracle import (exfat_entries, exfat_label, fat_attributes, fat_boot_label, fat_chains,  # noqa: E402
                            fat_label_and_serial, fat_short_names, parse_dump_exfat, parse_fsck_fat)
from fixture_system import configure_mtools, loop, mounted, run, volume_device  # noqa: E402

TOOLS = ("fsck.vfat", "fsck.exfat", "dump.exfat", "exfatlabel", "mdir", "mshowfat", "mattrib", "mlabel", "minfo", "losetup",
         "mount")
KINDS = {"fat12": "Fat12", "fat16": "Fat16", "fat32": "Fat32"}
EXFAT_FIELDS = ("clusters", "contiguous", "validDataLength", "attributes")
FSCK_ROUTINE = re.compile(r"^(fsck\.fat \d|exfatprogs version|/dev/\S+: (\d+ files, \d+/\d+ clusters|clean\.))")


def probe():
    missing = [tool for tool in TOOLS if run("which", tool, check=False).returncode != 0]
    if missing:
        return f"missing tools: {', '.join(missing)} (apt install dosfstools exfatprogs mtools util-linux)"
    if run("sudo", "-n", "true", check=False).returncode != 0:
        return "passwordless sudo is needed for loop devices and mounts"
    return None


def check(image, expected, work):
    problems = []
    start = expected.get("partitionStart") or 0
    is_exfat = expected["kind"] == "exfat"
    spec = SimpleNamespace(sector_size=expected["sectorSize"],
                           partition=SimpleNamespace(start=start) if start else None)

    with loop(image, spec.sector_size, partitioned=bool(start), read_only=True) as disk:
        device = volume_device(disk, spec)
        fsck = run("fsck.exfat" if is_exfat else "fsck.vfat", "-n", "-v", device, sudo=True, check=False)
        if fsck.returncode != 0:
            problems.append(f"fsck exited with {fsck.returncode}: {complaints(device, is_exfat)}")
        elif not is_exfat:
            kind = parse_fsck_fat(fsck.stdout)["kind"]
            if kind != KINDS[expected["kind"]]:
                problems.append(f"fsck sees {kind}, not {KINDS[expected['kind']]}")
        with mounted(device, "exfat" if is_exfat else "fat", work, read_only=True) as point:
            seen = read_back(point)
        dumped = []
        volume = None
        if is_exfat and not problems:
            dumped = [dict(e) for e in seen]
            try:
                exfat_entries(device, dumped)
            except RuntimeError as error:
                problems.append(f"dump.exfat: {error}")
            volume = parse_dump_exfat(run("dump.exfat", device, sudo=True).stdout)
            volume["label"] = exfat_label(device) or None
    if not is_exfat and not problems:
        volume = parse_fsck_fat(fsck.stdout)
        volume["label"], volume["volumeSerial"] = fat_label_and_serial(image, spec)
        volume["bootLabel"] = fat_boot_label(image, spec)

    wanted = {e["path"]: e for e in expected["entries"]}
    got = {e["path"]: e for e in seen}
    for path in sorted(set(wanted) - set(got)):
        problems.append(f"the kernel does not see {path}")
    for path in sorted(set(got) - set(wanted)):
        problems.append(f"the kernel sees {path}, which the library does not")
    for path in sorted(set(wanted) & set(got)):
        w, g = wanted[path], got[path]
        if w["type"] != g["type"]:
            problems.append(f"{path} is a {g['type']} to the kernel, a {w['type']} to the library")
        elif w["type"] == "file" and (w["size"] != g["size"] or w.get("sha256") != g.get("sha256")):
            problems.append(f"{path} differs: the kernel reads {g['size']} bytes {g.get('sha256')}, "
                            f"the library {w['size']} bytes {w.get('sha256')}")

    if not problems:
        problems += compare_volume(volume, expected.get("volume"))
        problems += compare_dump(dumped, wanted) if is_exfat else compare_mtools(image, spec, seen, wanted)
    return problems, fsck.stdout + fsck.stderr


def fsck_only(image, expected):
    """fsck's verdict alone: a problem for every line it prints beyond its banner and
    summary, since fsck.vfat -n reports some damage it will not repair with exit code 0."""
    start = expected.get("partitionStart") or 0
    is_exfat = expected["kind"] == "exfat"
    spec = SimpleNamespace(sector_size=expected["sectorSize"],
                           partition=SimpleNamespace(start=start) if start else None)
    with loop(image, spec.sector_size, partitioned=bool(start), read_only=True) as disk:
        device = volume_device(disk, spec)
        fsck = run("fsck.exfat" if is_exfat else "fsck.vfat", "-n", device, sudo=True, check=False)
    output = fsck.stdout + fsck.stderr
    lines = [line.strip() for line in output.splitlines() if line.strip() and not FSCK_ROUTINE.search(line)]
    if fsck.returncode == 0 and not lines:
        return [], output
    return [f"fsck exited with {fsck.returncode}: " + " | ".join(lines)], output


def compare_volume(seen, wanted):
    """The geometry, label, serial number and free clusters the tools read, against the
    library's; a value a tool does not report is not compared."""
    problems = []
    if not seen:
        return problems
    for key, value in (wanted or {}).items():
        if key in seen and (seen[key] is not None or key == "label") and seen[key] != value:
            problems.append(f"the tools see {key} {seen[key]!r}, the library {value!r}")
    return problems


def complaints(device, is_exfat):
    """What fsck finds, without the volume description -v adds."""
    brief = run("fsck.exfat" if is_exfat else "fsck.vfat", "-n", device, sudo=True, check=False)
    lines = (brief.stdout + brief.stderr).splitlines()[1:]
    return " | ".join(line.strip() for line in lines if line.strip())


def compare_dump(dumped, wanted):
    problems = []
    for entry in dumped:
        w = wanted[entry["path"]]
        for field in EXFAT_FIELDS:
            if field in w and w[field] != entry.get(field):
                problems.append(f"{entry['path']}: dump.exfat shows {field} {entry.get(field)}, the library {w[field]}")
    return problems


def compare_mtools(image, spec, seen, wanted):
    problems = []
    try:
        fat_short_names(image, spec, seen)
        fat_chains(image, spec, seen)
        fat_attributes(image, spec, seen)
    except RuntimeError as error:
        return [f"mtools: {error}"]
    for entry in seen:
        w = wanted[entry["path"]]
        if "shortName" in w and "shortName" in entry and w["shortName"].upper() != entry["shortName"].upper():
            problems.append(f"{entry['path']}: mdir shows the alias {entry.get('shortName')}, "
                            f"the library {w['shortName']}")
        if "attributes" in w and "attributes" in entry and w["attributes"] != entry["attributes"]:
            problems.append(f"{entry['path']}: mattrib shows the attributes {entry['attributes']:#04x}, "
                            f"the library {w['attributes']:#04x}")
        if "clusters" in w and "clusters" in entry and w["clusters"] != entry["clusters"]:
            problems.append(f"{entry['path']}: mshowfat shows the runs {entry['clusters']}, "
                            f"the library {w['clusters']}")
    return problems


def main():
    if sys.argv[1:] == ["--probe"]:
        reason = probe()
        print(reason or "ok")
        sys.exit(1 if reason else 0)
    arguments = sys.argv[1:]
    is_fsck_only = arguments[:1] == ["--fsck"]
    if is_fsck_only:
        arguments = arguments[1:]
    if len(arguments) != 2:
        sys.exit(__doc__)

    with open(arguments[1], encoding="utf-8") as f:
        expected = json.load(f)
    work = tempfile.mkdtemp(prefix="outwit-fat-check-")
    configure_mtools(work)
    try:
        # A loop device over a Windows drive is slow and not always allowed.
        image = os.path.join(work, "image.img")
        shutil.copyfile(arguments[0], image)
        problems, fsck = fsck_only(image, expected) if is_fsck_only else check(image, expected, work)
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print(json.dumps({"problems": problems, "fsck": fsck}, ensure_ascii=False))
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
