# Reference images

The FAT and exFAT images the tests read, and the script that makes them. Reading needs nothing
but .NET; making them needs Linux.

| | |
|---|---|
| `images/*.img.gz` | The images, gzip-compressed without timestamps. |
| `manifest.json` | What the Linux tools saw in each image: partition table, volume geometry, label, serial, free clusters, and every file and directory with its size, SHA-256 and modification time; on FAT, its 8.3 alias and cluster runs; on exFAT, its cluster runs (directories too), whether it is contiguous without a table chain, its valid data length and name hash, and where the root's bitmap and up-case table lie. `sha256` at image level is of the uncompressed image. |
| `make_fixtures.py` | Builds the images and the manifest. Image specifications are at its top. |
| `fixture_content.py` | The file tree every image carries, reading it back, and exFAT's file with an unwritten tail. |
| `fixture_oracle.py` | Parsers for `fsck.vfat -v`, `dump.exfat` (the boot region, and `-c -r -s /` for every entry set), `mshowfat`, `mdir`, `mattrib`, `minfo`, `mlabel`, `exfatlabel`, `sfdisk --json`. |
| `fixture_system.py` | Processes, loop devices, mounts. |
| `make-fixtures.ps1` | Runs the script in WSL from Windows. |
| `check_image.py` | Judges an image the library wrote, for the tests in the `Wsl` category. |

## Regenerating

From Windows, with the Ubuntu distribution in WSL:

```powershell
./make-fixtures.ps1                        # all images
./make-fixtures.ps1 fat16-c1 exfat-c8      # only these, merged into the manifest
```

Or inside Linux: `python3 make_fixtures.py [names...]`. It needs `dosfstools`, `exfatprogs`,
`mtools`, `util-linux` and passwordless `sudo` (for loop devices and mounts). A full run takes
about two minutes.

Regenerating changes the image bytes — the kernel stamps directories with the current time — so
commit the images and the manifest together.

## Judging what the library writes

`check_image.py IMAGE EXPECTED.json` takes an image and what the library believes it holds, and
checks it with tools that share no code with the library:

- `fsck.vfat -n` or `fsck.exfat -n` must be clean — on FAT both table copies agree, no lost or
  cross-linked clusters, FSInfo's free count right; on exFAT set checksums, name hashes, the
  bitmap against the chains, sizes against clusters;
- the kernel must list exactly those files and directories, with those sizes and hashes;
- on FAT, `mdir`, `mshowfat` and `mattrib` must show the same 8.3 aliases, cluster runs and
  attributes; on exFAT, `dump.exfat` the same cluster runs, contiguity, valid data lengths and
  attributes;
- the volume must have the same geometry, serial number, free clusters and label — on FAT the
  root's label as `mlabel` reads it and the boot sector's as `minfo` does, on exFAT as
  `exfatlabel` does.

It prints the problems as JSON. `check_image.py --fsck IMAGE EXPECTED.json` runs `fsck` alone,
for images the tests damaged on purpose, and counts every line `fsck` prints beyond its banner
and summary as a finding: `fsck.vfat` reports some damage it will not repair with exit code 0.

`Utils/WslOracle` runs it: through `wsl.exe` on Windows (distribution `Ubuntu`, or the one named
by `OUTWIT_FAT_WSL_DISTRIBUTION`), directly on Linux. The tests that use it are in the `Wsl`
category and are ignored where `check_image.py --probe` fails. To run only the rest:

```powershell
dotnet test --filter "TestCategory!=Wsl"
```

## How an image is made

1. A sparse file is attached as a loop device **with the image's own sector size**. For MBR
   images, `sfdisk` writes the table on that device.
2. `mkfs.vfat` or `mkfs.exfat` formats the volume; `tune.exfat` sets the exFAT serial.
3. FAT content is written by `mtools`; exFAT content by the kernel's driver.
4. The kernel adds, on both, the names `mtools` cannot write and two files grown a cluster at a
   time, alternately, so each is fragmented. On exFAT it also writes `/unwritten-tail.bin` and
   makes it longer with `truncate`, which leaves its valid data length where the writing stopped.
5. On exFAT, with the volume unmounted, the clusters of that file past its valid data length are
   filled with junk, where `dump.exfat` says they are. Only a reader that ignores the valid data
   length sees it; the kernel reads zeros there.
6. The volume is checked by `fsck.vfat -n` or `fsck.exfat -n` and must be clean.
7. The volume is mounted read-only and listed through the kernel. The listing must match what was
   written, byte for byte; it is what the manifest records.
8. Geometry and free clusters come from `fsck.vfat -v` or `dump.exfat`; on FAT, cluster runs come
   from `mshowfat` and 8.3 aliases from `mdir`; on exFAT, everything about entry sets comes from
   `dump.exfat -c -r -s /`.

## Things the tools do that are easy to miss

- **`sfdisk` on a plain file counts in 512-byte sectors** whatever `sector-size:` says. Tables on
  4Kn images are written through a loop device with `--sector-size 4096`.
- **`mkfs.vfat` takes hidden sectors from sysfs, which counts in 512-byte units** even on a 4Kn
  device, so a partition at sector 256 got 2048. The script passes `-h` explicitly.
- **`mtools` truncates characters outside the Basic Multilingual Plane** to 16 bits: an emoji
  became U+F600. Such names are written through the kernel, and `mshowfat` cannot address them,
  so their FAT entries carry no cluster runs.
- **`mshowfat` reports in directory order**, not argument order.
- **mtools writes short names in code page 850** unless told otherwise, while the kernel and the
  library use 437: an `Ï` came out as `╪`. The script gives mtools a configuration of its own
  (`MTOOLSRC`) with `default_codepage=437`.
- **`mdir` prints the 8.3 alias in lower case** when the entry's case flags say so; the manifest
  keeps what it prints, and the tests compare aliases without regard to case.
- **mtools does not take the smallest free numeric tail.** In a directory it fills, it passes
  over free numbers and hands them out later: `entry-259.dat` became `ENTRY~42` while `~41` was
  free, and `~41` went to a file created afterwards. The library takes the smallest free tail,
  so the tests compare an alias's base and extension, and whether it has a tail, not the number.
- **mtools puts code-page letters in aliases and adds no tail for them**: `Ünïcödé.txt` became
  `ÜNICÖDÉ.TXT`, and `日本語のファイル.txt` became `________.TXT`. The library keeps aliases
  in ASCII and, as the specification asks for a lossy name, adds a tail: `_N_C_D~1.TXT`,
  `______~1.TXT`.
- **`dump.exfat` does not print the number of FATs**; the manifest leaves it `null` rather than
  reading it from the bytes.
- **`mkfs.exfat` has no option for the serial number**; `tune.exfat -I` sets it afterwards.
- **`mkfs.vfat -F 32` accepts fewer than 65525 clusters** with a warning, and Linux mounts the
  result as FAT32. Two images keep that layout on purpose.
- **The kernel writes exFAT files in one piece without a table chain** (`NoFatChain`) and turns
  one into a chain when it can no longer grow in place; the fragmented files have chains, most
  others do not. An empty file has no cluster and only the `AllocationPossible` flag.
- **The kernel (6.8 and later) does not write what `truncate` adds** to an exFAT file: the valid
  data length stays behind, rounded up to the block — 4608 where 4196 bytes were written on a
  512-byte-sector image.
- **`dump.exfat -d PATH` prints no path**, so a listing is taken with `-c -r -s /`, whose sets
  are headed `Path:` or, for the root's own entries, `Directory:`. It prints only the low half of
  a timestamp, the time; the manifest takes times from the kernel instead.
- **A cluster chain is printed as `first:count` runs**, contiguous stretches of a table chain
  included; the manifest merges neighbouring runs into `[first, last]` pairs as `mshowfat` gives
  them.
