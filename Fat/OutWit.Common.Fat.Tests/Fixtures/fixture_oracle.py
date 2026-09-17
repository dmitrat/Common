"""What the Linux tools say about an image - the values the tests check against."""

import json
import re

from fixture_content import beyond_bmp
from fixture_system import loop, mtools_target, run


def parse_fsck_fat(text):
    """Geometry from `fsck.vfat -v`, which decides the FAT width the way the
    kernel does: 32 bits when the 16-bit table size is zero, otherwise by the
    cluster count."""
    def grab(expression):
        m = re.search(expression, text, re.MULTILINE)
        return int(m.group(1)) if m else None

    sector = grab(r"^\s*(\d+) bytes per logical sector")
    cluster = grab(r"^\s*(\d+) bytes per cluster")
    bits = grab(r"FATs, (\d+) bit entries")
    usage = re.search(r"\d+ files, (\d+)/(\d+) clusters", text)
    if not sector or not cluster or not bits or not usage:
        raise RuntimeError(f"unexpected fsck.vfat output:\n{text}")
    return {
        "freeClusters": int(usage.group(2)) - int(usage.group(1)),
        "kind": {12: "Fat12", 16: "Fat16", 32: "Fat32"}[bits],
        "sectorSize": sector,
        "sectorsPerCluster": cluster // sector,
        "totalSectors": grab(r"^\s*(\d+) sectors total"),
        "fatOffset": grab(r"First FAT starts at byte \d+ \(sector (\d+)\)"),
        "fatCount": grab(r"^\s*(\d+) FATs,"),
        "fatSectors": grab(r"bytes per FAT \(= (\d+) sectors\)"),
        "rootDirectorySector": grab(r"Root directory starts at byte \d+ \(sector (\d+)\)"),
        "rootDirectoryEntries": grab(r"^\s*(\d+) root directory entries"),
        "rootCluster": grab(r"Root directory start at cluster (\d+)"),
        "clusterHeapSector": grab(r"Data area starts at byte \d+ \(sector (\d+)\)"),
        "clusterCount": grab(r"^\s*(\d+) data clusters"),
    }


def parse_dump_exfat(text):
    """Geometry from `dump.exfat`. It does not print the number of FATs, so
    that stays unknown rather than being read from the bytes here."""
    def grab(label, base=10):
        m = re.search(rf"^{re.escape(label)}\s*:\s*(\S+)", text, re.MULTILINE)
        if not m:
            raise RuntimeError(f"dump.exfat lacks '{label}':\n{text}")
        return int(m.group(1), base)

    return {
        "freeClusters": grab("Free Clusters"),
        "kind": "ExFat",
        "sectorSize": grab("Bytes per Sector"),
        "sectorsPerCluster": grab("Sectors per Cluster"),
        "totalSectors": grab("Volume Length(sectors)"),
        "fatOffset": grab("FAT Offset(sector offset)"),
        "fatCount": None,
        "fatSectors": grab("FAT Length(sectors)"),
        "rootDirectorySector": None,
        "rootDirectoryEntries": None,
        "rootCluster": grab("Root Cluster (cluster offset)"),
        "clusterHeapSector": grab("Cluster Heap Offset (sector offset)"),
        "clusterCount": grab("Cluster Count"),
        "volumeSerial": grab("Volume Serial", 16),
    }


DUMP_ENTRY = re.compile(r"^\d+\. (.+) Directory Entry$")
DUMP_FIELD = re.compile(r"^\s+([A-Za-z0-9 ]+):\s+(.*)$")
DUMP_MORE = re.compile(r"^\s+(\d+:\d+)$")


def parse_dump_exfat_tree(text):
    """Entry sets from `dump.exfat -c -r -s /`: {path: {type: fields}} for files and
    directories, and the root's critical entries under the key None. A cluster chain
    is kept as its runs, "first:count"."""
    sets = {}
    current = None
    fields = None
    for line in text.splitlines():
        if line.startswith("Path: "):
            current = sets.setdefault(line[len("Path: "):], {})
            continue
        if line.startswith("Directory: "):
            current = sets.setdefault(None, {})
            continue
        m = DUMP_ENTRY.match(line.strip())
        if m and current is not None:
            fields = current.setdefault(m.group(1), {})
            continue
        m = DUMP_MORE.match(line)
        if m and fields is not None and "Cluster chain" in fields:
            fields["Cluster chain"].append(m.group(1))
            continue
        m = DUMP_FIELD.match(line)
        if m and fields is not None:
            key, value = m.group(1).strip(), m.group(2).strip()
            fields[key] = [value] if key == "Cluster chain" else value
    return sets


def chain_runs(runs):
    """"first:count" runs as [first, last] pairs, neighbours merged."""
    merged = []
    for run in runs:
        first, count = (int(x) for x in run.split(":"))
        if merged and merged[-1][1] + 1 == first:
            merged[-1][1] = first + count - 1
        else:
            merged.append([first, first + count - 1])
    return merged


def exfat_entries(device, entries):
    """What dump.exfat reads in each entry set: cluster runs of whatever has clusters,
    whether they are contiguous without a FAT chain, the valid data length, the name
    hash and the read-only, hidden, system and archive bits; and where the root's
    allocation bitmap and up-case table are."""
    sets = parse_dump_exfat_tree(run("dump.exfat", "-c", "-r", "-s", "/", device, sudo=True).stdout)
    for entry in entries:
        found = sets.get(entry["path"])
        stream = found and found.get("Stream Extension")
        if not stream:
            raise RuntimeError(f"dump.exfat did not show {entry['path']}")
        entry["contiguous"] = int(stream["GeneralSecondaryFlags"], 16) & 2 != 0
        entry["nameHash"] = int(stream["NameHash"], 16)
        entry["attributes"] = int(found["File"]["FileAttributes"], 16) & SETTABLE_ATTRIBUTES
        if entry["type"] == "file":
            entry["validDataLength"] = int(stream["ValidDataLength"])
        if int(stream["FirstCluster"]) != 0:
            entry["clusters"] = chain_runs(stream["Cluster chain"])
    root = sets.get(None, {})
    bitmap, upcase = root.get("Allocation Bitmap"), root.get("Up-case Table")
    if not bitmap or not upcase:
        raise RuntimeError(f"dump.exfat did not show the root's bitmap and up-case table: {root}")
    return {
        "bitmap": {"firstCluster": int(bitmap["FirstCluster"]), "length": int(bitmap["DataLength"]),
                   "clusters": chain_runs(bitmap["Cluster chain"])},
        "upcase": {"firstCluster": int(upcase["FirstCluster"]), "length": int(upcase["DataLength"]),
                   "checksum": int(upcase["TableChecksum"], 16), "clusters": chain_runs(upcase["Cluster chain"])},
    }


def fat_chains(image, spec, entries):
    """Cluster runs of every non-empty file, as mshowfat reports them.

    mshowfat reports in directory order, so lines are matched by path. Names
    outside the BMP are skipped: mtools cannot address them."""
    target = mtools_target(image, spec)
    files = [e for e in entries if e["type"] == "file" and e["size"] > 0 and not beyond_bmp(e["path"])]
    for i in range(0, len(files), 32):
        batch = {f"::{e['path']}": e for e in files[i:i + 32]}
        out = run("mshowfat", "-i", target, *batch).stdout
        for line in out.splitlines():
            keys = [k for k in batch if line.startswith(k + " ")]
            if not keys:
                continue
            key = max(keys, key=len)
            runs = re.findall(r"<(\d+)(?:-(\d+))?>", line[len(key):])
            batch[key]["clusters"] = [[int(a), int(b or a)] for a, b in runs]
        missing = [k for k, e in batch.items() if "clusters" not in e]
        if missing:
            raise RuntimeError(f"mshowfat did not report {missing}:\n{out}")


SETTABLE_ATTRIBUTES = 0x27
MATTRIB_FLAGS = {"R": 0x01, "H": 0x02, "S": 0x04, "A": 0x20}


def fat_attributes(image, spec, entries):
    """The read-only, hidden, system and archive bits of every entry, as mattrib shows
    them, a directory at a time. Names outside the BMP are skipped."""
    target = mtools_target(image, spec)
    directories = ["/"] + [e["path"] for e in entries if e["type"] == "directory"]
    by_path = {e["path"]: e for e in entries}
    for directory in directories:
        pattern = "::" + ("" if directory == "/" else directory) + "/*"
        out = run("mattrib", "-i", target, pattern, check=False).stdout
        for line in out.splitlines():
            at = line.find("::/")
            entry = by_path.get(line[at + 2:]) if at >= 0 else None
            if entry is not None:
                entry["attributes"] = sum(bit for flag, bit in MATTRIB_FLAGS.items() if flag in line[:at])
    missing = [e["path"] for e in entries if "attributes" not in e and not beyond_bmp(e["path"])]
    if missing:
        raise RuntimeError(f"mattrib did not show {missing[:5]}")


MDIR_LINE = re.compile(r"^(.{8}) (.{3})\s+(?:<DIR>|\d[\d ]*)\s+\d{4}-\d{2}-\d{2}\s+\d{1,2}:\d{2} (?: (.+))?$")


def fat_short_names(image, spec, entries):
    """The 8.3 alias of every entry, as mdir shows it (lower case where the entry's
    case flags say so). Names outside the BMP are skipped: mdir cannot print them."""
    target = mtools_target(image, spec)
    directories = ["/"] + [e["path"] for e in entries if e["type"] == "directory"]
    by_path = {e["path"]: e for e in entries}
    for directory in directories:
        out = run("mdir", "-a", "-i", target, "::" + directory).stdout
        prefix = "" if directory == "/" else directory
        for line in out.splitlines():
            m = MDIR_LINE.match(line)
            if not m:
                continue
            base, ext, long_name = m.group(1).rstrip(), m.group(2).rstrip(), m.group(3)
            if base in (".", ".."):
                continue
            short = base + ("." + ext if ext else "")
            entry = by_path.get(f"{prefix}/{long_name or short}")
            if entry is not None:
                entry["shortName"] = short
    missing = [e["path"] for e in entries if "shortName" not in e and not beyond_bmp(e["path"].rsplit("/", 1)[-1])]
    if missing:
        raise RuntimeError(f"mdir did not show {missing[:5]}")


def fat_label_and_serial(image, spec):
    target = mtools_target(image, spec)
    info = run("minfo", "-i", target, "::").stdout
    serial = int(re.search(r"serial number:\s*([0-9A-Fa-f]+)", info).group(1), 16)
    label_out = run("mlabel", "-s", "-i", target, "::").stdout
    m = re.search(r"Volume label is (.*)$", label_out, re.MULTILINE)
    return (m.group(1).rstrip() if m else None), serial


def fat_boot_label(image, spec):
    """The label the boot sector keeps, as minfo shows it."""
    info = run("minfo", "-i", mtools_target(image, spec), "::").stdout
    m = re.search(r'disk label="(.*)"', info)
    return m.group(1).rstrip() if m else None


def exfat_label(device):
    """The label, or None when there is none. exfatlabel fails on a root without a label
    entry, which the specification allows."""
    result = run("exfatlabel", device, sudo=True, check=False)
    m = re.search(r"^label:\s?(.*)$", result.stdout, re.MULTILINE)
    return m.group(1).rstrip() if m and result.returncode == 0 else None


def partition_table(image, spec):
    """The MBR as sfdisk reads it, in the image's own sector size."""
    if not spec.partition:
        return None, []
    with loop(image, spec.sector_size, partitioned=True, read_only=True) as disk:
        table = json.loads(run("sfdisk", "--json", disk, sudo=True).stdout)["partitiontable"]
    if table["sectorsize"] != spec.sector_size:
        raise RuntimeError(f"sfdisk reports sector size {table['sectorsize']}")
    partitions = [{
        "index": i,
        "type": int(p["type"], 16),
        "active": bool(p.get("bootable", False)),
        "firstSector": p["start"],
        "sectorCount": p["size"],
    } for i, p in enumerate(table["partitions"])]
    return int(table["id"], 16), partitions
