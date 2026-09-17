#!/usr/bin/env python3
"""Builds the reference FAT and exFAT images for OutWit.Common.Fat.Tests.

Runs inside WSL (Ubuntu) and needs dosfstools, exfatprogs, mtools, util-linux
and passwordless sudo for loop devices and mounts. Nothing here runs in the
tests themselves: they read the compressed images and manifest.json only.

Every image is written by one implementation and described by others:
mtools writes FAT content, the kernel writes exFAT content and the
fragmented files, fsck judges every volume, and the listing is read back
through the kernel's own driver - so the manifest records what an independent
reader sees in the image, not what this script meant to put there.

    python3 make_fixtures.py                 # all images
    python3 make_fixtures.py fat16-c1 ...    # only these, merged into the manifest
"""

import argparse
import gzip
import hashlib
import json
import os
import shutil
import sys
import tempfile
from dataclasses import dataclass

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from fixture_content import (UNWRITTEN_TAIL, build_content, compare, fragment,  # noqa: E402
                              poison_tail, read_back, unwritten_tail)
from fixture_oracle import (exfat_entries, exfat_label, fat_chains, fat_label_and_serial,  # noqa: E402
                            fat_short_names, parse_dump_exfat, parse_fsck_fat, partition_table)
from fixture_system import (check_prerequisites, configure_mtools, loop, mounted,  # noqa: E402
                            mtools_target, run, tool_versions, volume_device)

HERE = os.path.dirname(os.path.abspath(__file__))
MANIFEST = os.path.join(HERE, "manifest.json")
IMAGES_DIR = os.path.join(HERE, "images")
KINDS = {"fat12": "Fat12", "fat16": "Fat16", "fat32": "Fat32", "exfat": "ExFat"}


@dataclass
class Partition:
    start: int
    type: str
    bootable: bool = False


@dataclass
class ImageSpec:
    name: str
    kind: str
    sector_size: int
    sectors: int
    cluster_sectors: int
    label: str
    serial: int
    description: str
    partition: Partition | None = None
    fats: int = 2
    root_entries: int | None = None


IMAGES = [
    ImageSpec("fat12-floppy", "fat12", 512, 2880, 1, "FLOPPY", 0x1440F12A,
              "1.44 MB floppy, whole-disk volume, 224 root entries", root_entries=224),
    ImageSpec("fat12-mbr-c4", "fat12", 512, 16384, 4, "FAT12 MBR", 0x12C40001,
              "8 MiB disk, MBR partition type 01, 2 KiB clusters", partition=Partition(2048, "01")),
    ImageSpec("fat16-c1", "fat16", 512, 65536, 1, "FAT16 C1", 0x16C10001,
              "32 MiB whole-disk volume, one-sector clusters, near the FAT16 cluster limit"),
    ImageSpec("fat16-mbr-c16", "fat16", 512, 131072, 16, "FAT16 MBR", 0x16C16001,
              "64 MiB disk, active MBR partition type 06, 8 KiB clusters",
              partition=Partition(2048, "06", bootable=True)),
    ImageSpec("fat16-4k-1fat", "fat16", 4096, 16384, 1, "FAT16 4K", 0x164B0001,
              "64 MiB 4Kn disk, whole-disk volume, a single FAT copy", fats=1),
    ImageSpec("fat32-c1", "fat32", 512, 98304, 1, "FAT32 C1", 0x32C10001,
              "48 MiB whole-disk volume, one-sector clusters"),
    ImageSpec("fat32-mbr-c2", "fat32", 512, 163840, 2, "FAT32 MBR", 0x32C20001,
              "80 MiB disk, MBR partition type 0C, 1 KiB clusters", partition=Partition(2048, "0c")),
    ImageSpec("fat32-few-c64", "fat32", 512, 65536, 64, "FAT32 FEW", 0x32F64001,
              "32 MiB whole-disk FAT32 layout with 32 KiB clusters and fewer than 65525 of them; "
              "Linux mounts it as FAT32, a strict reading of the specification would not"),
    ImageSpec("fat32-4k-mbr", "fat32", 4096, 16384, 1, "FAT32 4K", 0x324B0001,
              "64 MiB 4Kn disk, MBR partition type 0C at sector 256, FAT32 layout below 65525 clusters",
              partition=Partition(256, "0c")),
    ImageSpec("exfat-c8", "exfat", 512, 32768, 8, "EXFAT C8", 0xEC080001,
              "16 MiB whole-disk volume, 4 KiB clusters"),
    ImageSpec("exfat-mbr-c1", "exfat", 512, 131072, 1, "EXFAT MBR", 0xEC010001,
              "64 MiB disk, MBR partition type 07, one-sector clusters", partition=Partition(2048, "07")),
    ImageSpec("exfat-4k-c4", "exfat", 4096, 16384, 4, "EXFAT 4K", 0xEC4B0001,
              "64 MiB 4Kn disk, whole-disk volume, 16 KiB clusters"),
    ImageSpec("exfat-mbr-c256", "exfat", 512, 131072, 256, "EXFAT BIG", 0xEC100001,
              "64 MiB disk, MBR partition type 07, 128 KiB clusters", partition=Partition(2048, "07")),
]


def format_volume(image, spec):
    with loop(image, spec.sector_size, partitioned=bool(spec.partition)) as disk:
        if spec.partition:
            p = spec.partition
            script = (f"label: dos\nlabel-id: 0x{spec.serial ^ 0x5A5A5A5A:08x}\nunit: sectors\n"
                      f"start={p.start}, type={p.type}{', bootable' if p.bootable else ''}\n")
            run("sfdisk", "--no-reread", "--no-tell-kernel", disk, sudo=True, input=script)
            run("partx", "--update", disk, sudo=True)
        target = volume_device(disk, spec)
        if spec.kind == "exfat":
            run("mkfs.exfat", "-c", spec.cluster_sectors * spec.sector_size, "-L", spec.label, target, sudo=True)
            run("tune.exfat", "-I", f"0x{spec.serial:08x}", target, sudo=True)
            return
        # Hidden sectors are passed explicitly: mkfs.vfat takes them from sysfs,
        # which counts in 512-byte units even on a 4Kn device.
        args = ["mkfs.vfat", "-F", spec.kind[3:], "-S", spec.sector_size, "-s", spec.cluster_sectors,
                "-f", spec.fats, "-i", f"{spec.serial:08X}", "-n", spec.label,
                "-h", spec.partition.start if spec.partition else 0]
        if spec.root_entries:
            args += ["-r", spec.root_entries]
        run(*args, target, sudo=True)


def populate(image, spec, staging, kernel_staging, work):
    """FAT content goes in through mtools, exFAT content through the kernel;
    the kernel then adds what mtools cannot write, the fragmented files and, on
    exFAT, a file with an unwritten tail."""
    kernel_roots = [kernel_staging]
    if spec.kind == "exfat":
        kernel_roots.insert(0, staging)
    else:
        names = [os.path.join(staging, n) for n in sorted(os.listdir(staging))]
        run("mcopy", "-s", "-m", "-i", mtools_target(image, spec), *names, "::/")
    with loop(image, spec.sector_size, bool(spec.partition)) as disk, \
            mounted(volume_device(disk, spec), spec.kind, work, read_only=False) as point:
        for root in kernel_roots:
            if os.listdir(root):
                run("cp", "-r", "--preserve=timestamps", "-T", root, point)
        written = fragment(point, spec.cluster_sectors * spec.sector_size)
        if spec.kind == "exfat":
            written.update(unwritten_tail(point, spec.cluster_sectors * spec.sector_size))
        return written


def poison_unwritten_tail(image, spec, size):
    """Fills what lies past the valid data length of UNWRITTEN_TAIL with junk, with the
    volume unmounted, where dump.exfat says its clusters are."""
    entry = {"path": UNWRITTEN_TAIL, "type": "file", "size": size}
    with loop(image, spec.sector_size, bool(spec.partition), read_only=True) as disk:
        device = volume_device(disk, spec)
        geometry = parse_dump_exfat(run("dump.exfat", device, sudo=True).stdout)
        exfat_entries(device, [entry])
    offset = spec.partition.start * spec.sector_size if spec.partition else 0
    poison_tail(image, offset, geometry, entry)


def describe(image, spec, work, files):
    with loop(image, spec.sector_size, bool(spec.partition), read_only=True) as disk:
        device = volume_device(disk, spec)
        if spec.kind == "exfat":
            fsck = run("fsck.exfat", "-n", "-v", device, sudo=True, check=False)
            geometry = parse_dump_exfat(run("dump.exfat", device, sudo=True).stdout)
            label = exfat_label(device)
        else:
            fsck = run("fsck.vfat", "-n", "-v", device, sudo=True, check=False)
            geometry = parse_fsck_fat(fsck.stdout)
            label = None
        if fsck.returncode != 0:
            raise RuntimeError(f"fsck is not clean on {spec.name}:\n{fsck.stdout}{fsck.stderr}")
        with mounted(device, spec.kind, work, read_only=True) as point:
            entries = read_back(point)
        if spec.kind == "exfat":
            geometry.update(exfat_entries(device, entries))

    compare(entries, files)
    if geometry["kind"] != KINDS[spec.kind]:
        raise RuntimeError(f"{spec.name}: the oracle sees {geometry['kind']}, not {KINDS[spec.kind]}")
    if spec.kind != "exfat":
        label, geometry["volumeSerial"] = fat_label_and_serial(image, spec)
        fat_chains(image, spec, entries)
        fat_short_names(image, spec, entries)
        for name in ("/frag/a.bin", "/frag/b.bin"):
            runs = next(e for e in entries if e["path"] == name)["clusters"]
            if len(runs) < 2:
                raise RuntimeError(f"{name} on {spec.name} is not fragmented: {runs}")
    geometry["label"] = label
    geometry["entries"] = entries
    return geometry


def pack(image, name):
    """Gzips the image without a timestamp and returns the raw image's SHA-256."""
    digest = hashlib.sha256()
    packed = os.path.join(IMAGES_DIR, name + ".img.gz")
    with open(image, "rb") as src, open(packed, "wb") as raw, \
            gzip.GzipFile(filename="", mode="wb", compresslevel=9, fileobj=raw, mtime=0) as dst:
        while chunk := src.read(1 << 20):
            digest.update(chunk)
            dst.write(chunk)
    return digest.hexdigest()


def build(spec, work):
    print(f"== {spec.name}", flush=True)
    image = os.path.join(work, spec.name + ".img")
    with open(image, "wb") as f:
        f.truncate(spec.sectors * spec.sector_size)
    staging = tempfile.mkdtemp(prefix="stage-", dir=work)
    kernel_staging = tempfile.mkdtemp(prefix="stage-kernel-", dir=work)
    files = build_content(staging, kernel_staging, spec.cluster_sectors * spec.sector_size)

    format_volume(image, spec)
    files.update(populate(image, spec, staging, kernel_staging, work))
    if spec.kind == "exfat":
        poison_unwritten_tail(image, spec, len(files[UNWRITTEN_TAIL]))
    disk_signature, partitions = partition_table(image, spec)
    volume = {
        "partitionIndex": 0 if partitions else None,
        "firstSector": partitions[0]["firstSector"] if partitions else 0,
        "sectorCount": partitions[0]["sectorCount"] if partitions else spec.sectors,
        **describe(image, spec, work, files),
    }
    digest = pack(image, spec.name)
    for path in (staging, kernel_staging):
        shutil.rmtree(path)
    os.remove(image)

    return {
        "name": spec.name,
        "file": f"images/{spec.name}.img.gz",
        "description": spec.description,
        "sha256": digest,
        "sectorSize": spec.sector_size,
        "sectorCount": spec.sectors,
        "partitionTable": "mbr" if partitions else "none",
        "diskSignature": disk_signature,
        "partitions": partitions,
        "volumes": [volume],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("only", nargs="*", help="image names to rebuild; default is all")
    args = parser.parse_args()

    check_prerequisites()
    unknown = set(args.only) - {s.name for s in IMAGES}
    if unknown:
        sys.exit(f"unknown images: {', '.join(sorted(unknown))}")
    specs = [s for s in IMAGES if not args.only or s.name in args.only]

    os.makedirs(IMAGES_DIR, exist_ok=True)
    previous = {}
    if args.only and os.path.exists(MANIFEST):
        with open(MANIFEST, encoding="utf-8") as f:
            previous = {i["name"]: i for i in json.load(f)["images"]}

    work = tempfile.mkdtemp(prefix="outwit-fat-")
    configure_mtools(work)
    try:
        built = {spec.name: build(spec, work) for spec in specs}
    finally:
        shutil.rmtree(work, ignore_errors=True)

    images = [i for i in (built.get(s.name) or previous.get(s.name) for s in IMAGES) if i]
    if not args.only:
        keep = {os.path.basename(i["file"]) for i in images}
        for name in os.listdir(IMAGES_DIR):
            if name not in keep:
                os.remove(os.path.join(IMAGES_DIR, name))

    write_manifest({"format": 1, "generator": "make_fixtures.py", "tools": tool_versions(), "images": images})
    print(f"wrote {len(images)} images to {MANIFEST}")


def write_manifest(manifest):
    """Indented JSON with one line per directory entry, so a listing of a few
    hundred files stays readable and diffs line by line."""
    listings = {}
    for image in manifest["images"]:
        for volume in image["volumes"]:
            key = f"@@entries-{len(listings)}@@"
            listings[key] = volume["entries"]
            volume["entries"] = key
    text = json.dumps(manifest, ensure_ascii=False, indent=2)
    for key, entries in listings.items():
        lines = ",\n".join(" " * 12 + json.dumps(e, ensure_ascii=False) for e in entries)
        text = text.replace(f'"{key}"', "[\n" + lines + "\n" + " " * 10 + "]")
    with open(MANIFEST, "w", encoding="utf-8", newline="\n") as f:
        f.write(text + "\n")


if __name__ == "__main__":
    main()
