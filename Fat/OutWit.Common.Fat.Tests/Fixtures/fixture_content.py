"""The file tree every reference image carries, and reading it back."""

import datetime
import hashlib
import os
import random

# Even seconds: FAT keeps modification times at two-second resolution.
FIXED_MTIME = datetime.datetime(2024, 2, 29, 12, 34, 56, tzinfo=datetime.timezone.utc).timestamp()
FRAGMENT_CLUSTERS = 6
EXTRA_DIRECTORIES = {"/Empty Directory", "/frag"}
UNWRITTEN_TAIL = "/unwritten-tail.bin"
JUNK = b"JUNK past the valid data length. "


def pattern(tag, size):
    """Bytes in which every 16-byte record names its own offset, so a cluster
    read from the wrong place cannot hash the same as the right one."""
    out = bytearray()
    offset = 0
    while len(out) < size:
        out += f"{tag[:4]:<4}{offset:010d}\r\n".encode("ascii")
        offset += 16
    return bytes(out[:size])


def beyond_bmp(text):
    return any(ord(c) > 0xFFFF for c in text)


def build_content(root, kernel_root, cluster_bytes):
    """Writes the standard tree and returns {path: bytes} for its files.

    Names outside the Basic Multilingual Plane go to kernel_root: mtools
    truncates them to 16 bits, so on FAT they are written by the kernel."""
    files = {}

    def put(rel, data):
        path = os.path.join(kernel_root if beyond_bmp(rel) else root, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "wb") as f:
            f.write(data)
        os.utime(path, (FIXED_MTIME, FIXED_MTIME))
        files["/" + rel] = data

    rng = random.Random(0x0F47)
    put("README.TXT", b"OutWit.Common.Fat reference image.\r\n")
    put("lower.txt", b"A lower-case short name.\n")
    put("empty.bin", b"")
    put("one.bin", b"\x5a")
    put("cluster.bin", pattern("clus", cluster_bytes))
    put("cluster-plus-one.bin", pattern("clp1", cluster_bytes + 1))
    put("Mixed Case Name.txt", b"Mixed case needs a long name.\n")
    put("a.b.c.d.txt", b"Several dots.\n")
    put("A name that is quite a bit longer than thirteen characters.txt", pattern("long", 3000))
    for i in range(1, 4):
        put(f"Long File Name {i}.txt", f"Numeric tail {i}.\n".encode("ascii"))
    put("unicode/Привет мир.txt", "Кириллица.\n".encode("utf-8"))
    put("unicode/日本語のファイル.txt", "日本語。\n".encode("utf-8"))
    put("unicode/emoji \U0001F600.txt", "Outside the BMP.\n".encode("utf-8"))
    put("unicode/Ünïcödé.txt", "Latin with marks.\n".encode("utf-8"))
    put("max/" + "n" * 251 + ".txt", b"A name of 255 characters.\n")
    deep = "/".join(f"level{i:02d}" for i in range(1, 13))
    put(deep + "/leaf.txt", b"Twelve levels down.\n")
    for i in range(300):
        put(f"many/entry-{i:03d}.dat", b"")
    put("data/random.bin", bytes(rng.getrandbits(8) for _ in range(40000)))
    os.makedirs(os.path.join(root, "Empty Directory"))
    return files


def fragment(mount_point, cluster_bytes):
    """Grows two files a cluster at a time, alternately, so each is split.

    Both kernel drivers allocate at write time, so the chains interleave."""
    os.makedirs(os.path.join(mount_point, "frag"))
    data = {"/frag/a.bin": pattern("frgA", cluster_bytes * FRAGMENT_CLUSTERS),
            "/frag/b.bin": pattern("frgB", cluster_bytes * FRAGMENT_CLUSTERS)}
    paths = {key: mount_point + key for key in data}
    handles = {key: open(path, "wb") for key, path in paths.items()}
    try:
        for k in range(FRAGMENT_CLUSTERS):
            for key, handle in handles.items():
                handle.write(data[key][k * cluster_bytes:(k + 1) * cluster_bytes])
                handle.flush()
                os.fsync(handle.fileno())
    finally:
        for handle in handles.values():
            handle.close()
    for path in paths.values():
        os.utime(path, (FIXED_MTIME, FIXED_MTIME))
    return data


def unwritten_tail(mount_point, cluster_bytes):
    """exFAT only: a file longer than what was written to it. The kernel grows a file by
    truncate without writing the new part, so its valid data length stays near where the
    writing stopped and the rest reads as zeros; poison_tail then fills that rest with junk
    on the device, where only a reader that ignores the valid data length would see it."""
    written = pattern("tail", cluster_bytes + 100)
    length = cluster_bytes * 3 + 7
    path = mount_point + UNWRITTEN_TAIL
    with open(path, "wb") as f:
        f.write(written)
    os.truncate(path, length)
    os.utime(path, (FIXED_MTIME, FIXED_MTIME))
    return {UNWRITTEN_TAIL: written + bytes(length - len(written))}


def poison_tail(image, volume_offset, geometry, entry):
    """Writes junk into the clusters of UNWRITTEN_TAIL past its valid data length."""
    valid, runs = entry["validDataLength"], entry["clusters"]
    if valid >= entry["size"]:
        raise RuntimeError(f"{UNWRITTEN_TAIL} has valid data to its end ({valid}); nothing is unwritten")
    sector = geometry["sectorSize"]
    cluster_bytes = geometry["sectorsPerCluster"] * sector
    heap = volume_offset + geometry["clusterHeapSector"] * sector
    position = 0
    with open(image, "r+b") as f:
        for first, last in runs:
            for cluster in range(first, last + 1):
                start = max(valid - position, 0)
                if start < cluster_bytes:
                    junk = (JUNK * (cluster_bytes // len(JUNK) + 1))[start:cluster_bytes]
                    f.seek(heap + (cluster - 2) * cluster_bytes + start)
                    f.write(junk)
                position += cluster_bytes


def read_back(mount_point):
    """Lists the volume as the kernel sees it."""
    entries = []
    for dirpath, dirnames, filenames in os.walk(mount_point):
        dirnames.sort()
        rel_dir = os.path.relpath(dirpath, mount_point)
        prefix = "" if rel_dir == "." else "/" + rel_dir.replace(os.sep, "/")
        for d in dirnames:
            entries.append({"path": f"{prefix}/{d}", "type": "directory"})
        for name in sorted(filenames):
            full = os.path.join(dirpath, name)
            with open(full, "rb") as f:
                data = f.read()
            mtime = datetime.datetime.fromtimestamp(os.stat(full).st_mtime, datetime.timezone.utc)
            entry = {"path": f"{prefix}/{name}", "type": "file", "size": len(data)}
            if data:
                entry["sha256"] = hashlib.sha256(data).hexdigest()
            entry["modified"] = mtime.strftime("%Y-%m-%dT%H:%M:%SZ")
            entries.append(entry)
    entries.sort(key=lambda e: e["path"])
    return entries


def compare(entries, expected_files):
    """Fails unless the kernel sees exactly the files that were meant."""
    empty = hashlib.sha256(b"").hexdigest()
    seen_files = {e["path"]: e.get("sha256", empty) for e in entries if e["type"] == "file"}
    wanted = {p: hashlib.sha256(d).hexdigest() for p, d in expected_files.items()}
    if seen_files != wanted:
        missing = sorted(set(wanted) - set(seen_files))
        extra = sorted(set(seen_files) - set(wanted))
        changed = sorted(p for p in set(wanted) & set(seen_files) if wanted[p] != seen_files[p])
        raise RuntimeError(f"read-back differs: missing {missing}, extra {extra}, changed {changed}")

    wanted_dirs = set(EXTRA_DIRECTORIES)
    for path in expected_files:
        parts = path.split("/")[1:-1]
        for i in range(1, len(parts) + 1):
            wanted_dirs.add("/" + "/".join(parts[:i]))
    seen_dirs = {e["path"] for e in entries if e["type"] == "directory"}
    if seen_dirs != wanted_dirs:
        raise RuntimeError(f"read-back directories differ: missing {sorted(wanted_dirs - seen_dirs)}, "
                           f"extra {sorted(seen_dirs - wanted_dirs)}")
