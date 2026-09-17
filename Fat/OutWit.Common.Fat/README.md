# OutWit.Common.Fat

## Overview

OutWit.Common.Fat is a FAT12, FAT16, FAT32 and exFAT library for .NET that works over a
**pluggable, asynchronous block device**. It is built for media that are slow or far away — a
memory card behind a radio link, where every sector is a request and an answer — so it reads
what it needs when it needs it, in batches, and never walks the whole allocation table at mount.

This release contains the block device contract, the devices, volume detection (MBR or
whole-disk; FAT12/16/32 and exFAT from the boot sector), reading and writing all four kinds —
directories, long names, files, attributes and the volume label — formatting them, and a
consistency checker. How it is known to be right is told at the end, under *How it is tested*.

`FatVolume`, `FatDetector`, `FatFormatter` and `FatChecker` are in `OutWit.Common.Fat`; what they take and
give back — the layouts, entries, problems, options and enums — in `OutWit.Common.Fat.Model`; the devices
in `OutWit.Common.Fat.Devices`; `FatException` in `OutWit.Common.Fat.Exceptions`; and the shortcuts over
volumes and devices in `OutWit.Common.Fat.Utils`.

## Block devices

```csharp
public interface IBlockDevice : IAsyncDisposable
{
    int SectorSize { get; }
    long SectorCount { get; }
    bool IsReadOnly { get; }

    ValueTask ReadAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
```

A call covers as many consecutive sectors as the buffer holds. Sector sizes are powers of two
from 512 to 4096 — the sizes FAT and exFAT allow. Calls are not concurrent: the caller awaits one
before starting the next.

| Device | Use |
|---|---|
| `BlockDeviceMemory` | Tests and images held in memory. Sparse: only chunks that held non-zero data take memory. `LoadAsync` reads any stream, including a decompressing one; `SaveAsync` writes the image back. |
| `BlockDeviceFile` | Image files, with positional I/O. |
| `BlockDeviceStream` | Any readable, seekable stream. |
| `BlockDeviceCached` | A least-recently-used sector cache in front of another device. Misses are fetched in runs; writes are held until a flush and then written in ascending order, one request per run of consecutive sectors — or, created with `writesThrough`, passed on at once in the order they were made, so that only reads are saved. Room for a write is made before any of it is applied, so a write the cache can hold is applied whole or not at all. |
| `BlockDevicePartition` | A window onto part of another device — one partition seen as a device. Does not own the disk. |

Deriving from `BlockDeviceBase` gives argument validation, cancellation and disposal for free: a
derived device only implements `ReadSectorsAsync` and `WriteSectorsAsync`, and sees only valid,
non-empty requests.

## Detecting volumes

```csharp
await using var disk = BlockDeviceFile.Open("card.img", isReadOnly: true);
await using var cached = new BlockDeviceCached(disk, capacity: 4096);

FatDiskLayout layout = await FatDetector.DetectDiskAsync(cached);
foreach (var location in layout.Volumes)
{
    Console.WriteLine($"{location.Volume.Kind} at sector {location.FirstSector}, " +
                      $"{location.Volume.ClusterCount} clusters of {location.Volume.ClusterSize} bytes");

    await using var volume = new BlockDevicePartition(cached, location.FirstSector, location.SectorCount);
    // ...
}
```

- `DetectDiskAsync` reads sector zero as a boot sector first and as a master boot record second,
  as FatFs does — unless the boot sector is a copy of the first partition's parameters (its
  hidden-sector count names a partition in the table), in which case the table wins. Every
  primary partition is probed whatever its type byte says. Extended partitions and empty slots
  are listed but not followed; a GUID partition table is reported as unsupported.
- `DetectVolumeAsync` describes the volume at sector zero of a device, or returns `null`.
- Recognition costs one request for FAT and two for exFAT, whose twelve-sector boot region
  checksum is verified before anything else is trusted.

"Not a FAT volume" and "a broken FAT volume" are kept apart: the first is `null` or an absent
entry; the second is a `FatException` whose `Kind` says what went wrong — `Corrupt`,
`Unsupported`, or `SectorSizeMismatch` when the volume's sector size differs from the device's.
On a partitioned disk a bad partition does not hide the others: it is listed in
`FatDiskLayout.Problems` with the same kind and message.

The FAT width is settled between the Linux driver and FatFs, the implementation embedded devices
run. A 16-bit table size of zero means FAT32 whatever the cluster count, as in Linux. Otherwise up
to 4084 clusters is FAT12 and from 4086 FAT16; at exactly 4085, where the two disagree, the table
decides — FAT16 if it can hold 16-bit entries for all of them. FAT16 goes up to 65525 clusters, as
in FatFs. A data area larger than the table can address is accepted, as in Linux, and the clusters
past the table are left out of the count.

## Reading a volume

```csharp
await using var card = new BlockDeviceCached(recorderCard, capacity: 4096);
await using var volume = await FatVolume.MountDiskAsync(card);

Console.WriteLine($"{volume.Info.Kind}, label {await volume.GetLabelAsync()}");

await foreach (var entry in volume.EnumerateAsync("/", recursive: true))
    Console.WriteLine($"{entry.Path} {entry.Length} {entry.Modified}");

byte[] config = await volume.ReadAllBytesAsync("/nrconfig.nrs");   // FatVolumeExtensions

await using var record = await volume.OpenReadAsync("/Records/nrrecord.nrr");
record.Position = 1 << 20;
await record.ReadExactlyAsync(buffer);
```

- `MountAsync` takes a device that holds one volume; `MountDiskAsync` takes a disk and mounts
  its first volume, of any of the four kinds. Mounting reads the boot sector — exFAT's boot
  region — and nothing more. `FatVolumeOptions` says whether the device stays open when the
  volume is disposed, and which clock stamps new entries.
- Directories are read as far as they are needed and stop at their end marker: a sector first,
  then twice as many each time, so a small directory costs one request however large its clusters. The allocation
  table is read an entry at a time; a few table sectors are kept, so a chain costs a request per
  table sector, not per cluster. A file read follows the chain only as far as it reads and reads
  each contiguous stretch in one request.
- Paths take `/` or `\`, rooted either way; names match regardless of case, by long name or by
  8.3 alias, and on exFAT through the volume's own up-case table. `GetEntryAsync` returns `null`
  for nothing there; opening or listing the wrong kind of entry throws `FatException` with
  `NotFound`, `NotADirectory` or `NotAFile`.
- `FatDirectoryEntry` carries the long name, the 8.3 alias as stored (none on exFAT), attributes,
  length, first cluster and the three times. FAT keeps local time with no zone, so its times are
  of `DateTimeKind.Unspecified`; exFAT records each time's offset from UTC, and a time that has
  one is given in UTC. Short names are decoded in code page 437, as Linux does by default.
- On exFAT a file whose clusters follow each other may have no chain in the table, and is read
  without touching it; what lies past a file's valid data length reads as zeros without a
  request to the device. A directory is as long as its entry says. The up-case table and the
  allocation bitmap are read from the root when they are first needed — the bitmap only by
  `CountFreeClustersAsync`.
- A chain that loops, leaves the volume, or ends before its file does is reported as `Corrupt`
  when it is reached, and so is an exFAT entry set whose checksum or parts are wrong.

## Writing to a volume

```csharp
await using var volume = await FatVolume.MountDiskAsync(card);

await volume.CreateDirectoryAsync("/Records/2026-09-16");
await volume.WriteAllBytesAsync("/nrconfig.nrs", config);

await using (var log = await volume.OpenAsync("/Records/events.log", FileMode.Append, FileAccess.Write))
    await log.WriteAsync(line);

await volume.MoveAsync("/Records/2026-09-16", "/Archive/2026-09-16");
await volume.DeleteAsync("/Archive", recursive: true);
```

- `OpenAsync` takes every `FileMode` and `FileAccess` and behaves as `FileStream` does: `Create`
  empties an existing file, `Append` writes only past the end the file had, `CreateNew` refuses
  an existing one, and a mode that writes refuses read-only access. `CreateAsync` and
  `WriteAllBytesAsync` are shortcuts.
- A file may be open for reading any number of times or for writing once; anything else, and
  deleting or moving an open file, is `InUse`. A file with the read-only attribute is not
  written, deleted or replaced: `AccessDenied`.
- A growing file takes clusters next to the ones it has where it can, and the search for a free
  cluster starts where the last one ended — on FAT32 where FSInfo says. Nothing walks the whole
  table unless the volume is full or `CountFreeClustersAsync` is asked. FAT has no sparse files:
  a write past the end, or a longer length, fills the gap with zeros. A file holds at most
  4 GiB less one byte (`TooLarge`).
- The allocation table is changed in memory and written — to every copy, or to the active one
  when mirroring is off — when a stream is flushed or disposed, when an operation ends, or when a
  changed table sector has to make room. FSInfo's free count and hint are kept and written
  back.
- `CreateDirectoryAsync`, `DeleteAsync` and `MoveAsync` flush before they return; a recursive
  delete that stops at an open or read-only entry has deleted what came before it, and flushes
  that much before it reports why it stopped. Writes to a
  file reach its directory entry on the stream's `FlushAsync` or disposal. The order of writes
  is chosen so that a crash leaves at worst clusters that belong to no one: a file's new
  clusters are linked before its entry counts on them, and the clusters it gives up are released
  after; a moved entry is written in its new place before the old one is removed. That holds on
  devices that write in the order they are asked to; `BlockDeviceCached` does so only when
  created with `writesThrough`, which is the choice for a device that may be cut off mid-way.
- New names follow Windows and Linux. A name that is already a valid 8.3 name, its base and
  extension each in one case, is stored as a short entry alone with the case flags Windows NT
  introduced; any other gets a long name and an ASCII alias with the smallest free numeric
  tail, such as `LONGFI~1.TXT`. Names match regardless of case, so a name taken in any case, or
  as another entry's alias, is `AlreadyExists`. Renaming to another case is allowed.
- A full volume is `NoSpace`, and so is a full FAT12/16 root directory or a directory at 65,536
  entries. A write that fails for lack of room takes nothing and leaves the file as it was; one
  the device fails in the middle of gives back the clusters it had taken.
- Moving a directory to another parent points its `..` entry at the new one; a directory cannot
  move into itself. `MoveAsync` replaces a file only when asked, and never a directory.
- `SetAttributesAsync` sets read-only, hidden, system and archive, and clears the ones not
  given; what tells a directory from a file is kept, and no time changes. A write sets the
  archive bit again.
- `SetLabelAsync` sets or removes the label. On FAT12/16/32 the root's label entry and the boot
  sector's copy — and FAT32's backup boot sector — are changed together, as Windows and
  `fatlabel` keep them; the label is up to 11 ASCII characters an 8.3 name may hold, stored in
  upper case. On exFAT it is up to 11 UTF-16 units, and removing it leaves the label entry with
  no characters, as `mkfs.exfat` writes it.
- A volume on a read-only device refuses every change with `NotSupportedException`.

On exFAT, writing follows what Linux does:

- A new file or directory takes its clusters without a table chain when they follow each other,
  and keeps that form while new clusters follow the old; the first time they do not, a chain is
  written through all of them. Free clusters are those the allocation bitmap marks free.
- Files are not limited to 4 GiB. A write past a file's valid data length fills only the gap with
  zeros; a longer length set on its own writes nothing, and the new part reads as zeros.
- A new directory gets one cluster, its size in its entry; a directory that grows records its new
  size in its parent. Every entry set is written whole, with its checksum and a name hash made
  with the volume's up-case table.
- The first change marks the volume dirty in its boot sector; `FlushAsync` and disposal clear the
  mark — unless the volume was dirty when mounted — and record the percentage in use once the
  free clusters have been counted.

## Formatting

```csharp
await FatFormatter.FormatAsync(image, new FatFormatOptions { Label = "DATA" });

FatDiskLayout disk = await FatFormatter.FormatDiskAsync(card, new FatFormatOptions
{
    Kind = FatKind.Fat32,
    ClusterSize = 4096
});
```

- `FormatAsync` makes the whole device one volume; `FormatDiskAsync` writes a master boot record
  with one partition from 1 MiB to the end of the disk, of the type Windows gives the kind, and
  formats that. Both return what `FatDetector` then reads back.
- Left to itself, the formatter chooses as Windows and card makers do: FAT12 up to 8 MiB, FAT16
  up to 512 MiB, FAT32 up to 32 GiB and exFAT above, each with the usual cluster size for its
  size. When the choice cannot be made on the device — a small volume of 4096-byte sectors has
  too few clusters for FAT16 — the other kinds are tried, from FAT12 up, and the first that fits
  is taken; a kind that was asked for is never changed, and a volume that cannot be made is
  `ArgumentException`.
- FAT12/16/32 volumes get two table copies unless asked for one, a 512-entry root on FAT12/16,
  FSInfo and a backup boot sector on FAT32; exFAT volumes get the bitmap, the up-case table
  Windows writes, a root with a label entry, and the backup boot region. The serial number comes
  from the clock unless given.
- Cluster counts stay where every reader agrees on the kind: FAT12 up to 4084 clusters, FAT16
  from 4087 — Linux reads 4085 and 4086 as FAT16 and Windows as FAT12, so `mkfs.fat` makes
  neither — and FAT32 from 65525.
- The data area is not cleared. The old boot sector and exFAT's two boot regions are cleared
  first and the new boot sector written last; a disk's partition table is cleared first and
  written after its volume. A format cut short leaves nothing that passes for a volume, old or
  new.

## Checking

```csharp
FatCheckReport report = await FatChecker.CheckAsync(volume);
if (!report.IsClean)
    foreach (var problem in report.Problems)
        Console.WriteLine($"{problem.Kind} {problem.Path} {problem.Cluster}: {problem.Message}");
```

`FatChecker` reads the whole volume and changes nothing. It walks every directory and every
chain, and compares the allocation table — or exFAT's bitmap — with what it found. It reports,
as a `FatProblemKind` with the path and cluster concerned:

- chains that break, leave the volume or loop, clusters shared by two entries, lengths that do
  not match their clusters, and clusters marked used that belong to nothing;
- on exFAT, a bitmap that marks free a cluster in use, entry sets that are broken, name hashes
  that are wrong, and a bitmap or up-case table that is missing or does not match its entry;
- on FAT, long names that belong to no entry, `.` and `..` entries that point elsewhere, a
  directory that holds one of its own ancestors, table copies that differ, and an FSInfo free
  count that is wrong;
- a backup boot sector or region that differs from the main one, and a volume left dirty.

The report also counts files, directories and used, lost, bad and free clusters; a cluster
marked bad is not lost, and on a volume with two exFAT tables both bitmaps are accounted for.
Problems of one kind stop being listed after a hundred. A volume mounted for writing is flushed
first. The checker throws only when the device fails; damage, however bad, is reported. It
keeps four bytes for each cluster in use and follows every cluster once, whatever the damage;
a directory under a path longer than 32,767 characters is reported and not entered.

The checker finds what `fsck.vfat -n` and `fsck.exfat -n` find on the same damage, and a little
more: `fsck.exfat` does not report lost clusters, a dirty volume or a broken backup boot region.
It does not report a stale exFAT percentage in use, which Linux never updates.

## Disposal

Devices are released with `DisposeAsync`; a volume releases the device it was given unless
`FatVolumeOptions.LeaveOpen` is set. Disposing a volume first flushes and closes the files still
open for writing. If that flush fails, the volume stays open and disposal can be retried; after
it, the volume refuses work, and streams still open for reading fail. When releasing a device
fails — a cache whose final flush cannot be written — the exception propagates and the device
stays open with everything it holds, so the disposal can be retried.

## How it is tested

Silent corruption is the failure mode, so the tests lean on independent implementations. Reference
images of every kind — several cluster sizes, 512- and 4096-byte sectors, whole-disk and MBR,
long and Unicode names, deep and large directories, fragmented files — are made in Linux by
`mkfs.vfat`, `mkfs.exfat`, `sfdisk`, `mtools` and the kernel's own drivers, judged clean by `fsck`,
and described by those tools and `dump.exfat` in a manifest — down to cluster runs, exFAT's
contiguous files, valid data lengths and name hashes. The images are committed compressed;
reading them needs no Linux.

Writing is judged the same way. Tests change every reference image — new, long and Unicode
names, growing directories, deletes, moves across directories, overwrites, truncation, two files
written in turns, files grown past what was written to them, a volume filled to the last cluster
— and hand the result to `fsck.vfat` or `fsck.exfat`, the kernel's driver, `mdir`, `mshowfat`
and `dump.exfat`, which must find it clean and see exactly the files, hashes, aliases, cluster
runs, contiguity, valid lengths, attributes and labels the library reports. Volumes the library
formats go through the same tools, empty and filled. Volumes damaged on purpose, one structure
at a time, must be reported by the checker exactly — and by `fsck`, where `fsck` reports that
damage at all. Those tests run where WSL, or Linux with the tools, is present. Everywhere else,
random sequences of changes are checked against a model of what the volume should hold, every
cluster is accounted for, and the checker must find every reference image and every changed
volume clean.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Fat in a product, a mention is appreciated
(but not required): "Powered by OutWit.Common.Fat (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by
Dmitry Ratner. You may refer to the project name in a factual way (e.g.,
"built with OutWit.Common.Fat") or to indicate compatibility. You may
not use the name as the name of a fork or derived product in a way that
implies it is the official project, nor use the OutWit logo to promote
forks or derived products without permission.
