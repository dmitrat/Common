"""Processes, loop devices and mounts for make_fixtures.py."""

import contextlib
import os
import subprocess
import sys
import tempfile

# mtools converts timestamps with TZ and the kernel is mounted with tz=UTC,
# so both sides agree on the wall-clock time stored in FAT.
ENV = dict(os.environ, TZ="UTC", LANG="C.UTF-8", LC_ALL="C.UTF-8", MTOOLS_SKIP_CHECK="1")


def configure_mtools(work):
    """Points mtools at a configuration of its own. Code page 437 is what the kernel
    uses for short names by default, and what the library decodes them with; mtools
    would otherwise write them in 850."""
    path = os.path.join(work, "mtoolsrc")
    with open(path, "w", encoding="ascii") as f:
        f.write("default_codepage=437\nmtools_skip_check=1\n")
    ENV["MTOOLSRC"] = path


def run(*args, sudo=False, check=True, input=None):
    cmd = (["sudo", "-n"] if sudo else []) + [str(a) for a in args]
    result = subprocess.run(cmd, env=ENV, input=input, capture_output=True, text=True)
    if check and result.returncode != 0:
        raise RuntimeError(f"{' '.join(cmd)} failed ({result.returncode}):\n{result.stdout}{result.stderr}")
    return result


@contextlib.contextmanager
def loop(image, sector_size, partitioned, read_only=False):
    """Attaches an image with the given logical sector size.

    Partition tables on 4Kn images must be written through a loop device:
    sfdisk on a plain file counts in 512-byte sectors whatever it is told."""
    args = ["losetup", "--find", "--show", "--sector-size", sector_size]
    if partitioned:
        args.append("--partscan")
    if read_only:
        args.append("--read-only")
    device = run(*args, image, sudo=True).stdout.strip()
    try:
        yield device
    finally:
        run("blockdev", "--flushbufs", device, sudo=True, check=False)
        run("losetup", "--detach", device, sudo=True, check=False)


@contextlib.contextmanager
def mounted(device, kind, work, read_only):
    point = tempfile.mkdtemp(prefix="mnt-", dir=work)
    options = [f"uid={os.getuid()}", f"gid={os.getgid()}", "ro" if read_only else "rw"]
    if kind == "exfat":
        fstype = "exfat"
    else:
        fstype = "vfat"
        options += ["utf8=1", "tz=UTC", "shortname=mixed"]
    run("mount", "-t", fstype, "-o", ",".join(options), device, point, sudo=True)
    try:
        yield point
    finally:
        run("umount", point, sudo=True)
        os.rmdir(point)


def volume_device(disk, spec):
    return f"{disk}p1" if spec.partition else disk


def mtools_target(image, spec):
    offset = spec.partition.start * spec.sector_size if spec.partition else 0
    return f"{image}@@{offset}" if offset else image


def tool_versions():
    def package(name):
        out = run("dpkg-query", "-W", "-f=${Version}", name, check=False).stdout.strip()
        return out or "unknown"

    return {
        "dosfstools": package("dosfstools"),
        "exfatprogs": package("exfatprogs"),
        "mtools": package("mtools"),
        "util-linux": package("util-linux"),
        "kernel": os.uname().release,
    }


def check_prerequisites():
    if not sys.platform.startswith("linux"):
        raise SystemExit("run this inside WSL or another Linux, not on Windows")
    missing = [tool for tool in ("mkfs.vfat", "fsck.vfat", "mkfs.exfat", "fsck.exfat", "dump.exfat",
                                 "tune.exfat", "exfatlabel", "mcopy", "mshowfat", "minfo", "mlabel",
                                 "sfdisk", "partx", "losetup")
               if run("which", tool, check=False).returncode != 0]
    if missing:
        raise SystemExit(f"missing tools: {', '.join(missing)} "
                         "(apt install dosfstools exfatprogs mtools util-linux)")
    if run("sudo", "-n", "true", check=False).returncode != 0:
        raise SystemExit("passwordless sudo is needed for loop devices and mounts")
