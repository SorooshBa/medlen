# Media Duration CLI — Plan

## Goal

Build a small command-line application that scans the current directory (and its
subdirectories) for media files, reads each file's duration, and reports:

- the duration of every detected media file;
- the combined duration of all files;
- the average duration;
- the longest file; and
- the shortest file.

The application will not depend on FFmpeg or other large native multimedia
frameworks.

## Technical approach

Use C# with modern .NET (the current .NET LTS target) to build a small,
cross-platform console application. Keep dependencies at zero where practical;
use small, format-specific parsers only if a format cannot be read safely in a
small amount of code.

Media duration is container/format metadata, so no lightweight solution can
reliably support every media format in existence. The first version will support
common formats with direct metadata parsing:

| Format | Method |
| --- | --- |
| WAV | Read the RIFF header: data size, sample rate, channels, and bit depth. |
| MP3 | Parse frame headers and, when present, Xing/VBRI metadata. |
| MP4/M4A/MOV | Read MP4 atoms, especially `mvhd` timescale and duration. |
| OGG/Opus | Read stream headers and the final granule position. |
| FLAC | Read the STREAMINFO metadata block. |

Files with a recognized extension but no supported/valid duration metadata will
be listed as skipped with a reason. They will not affect the statistics.

## Command behavior

Initial command shape:

```text
medlen [path]
```

- With no `path`, scan the current working directory.
- With `path`, scan that directory.
- Walk directories recursively, ignore directories and unreadable files, and
  avoid following symlinks by default.
- Sort output by relative path for repeatable results.
- Display durations in a human-friendly `HH:MM:SS.mmm` format.
- Return a non-zero exit code only for command-level failures (for example, an
  invalid root path). Individual unreadable or unsupported files are warnings.

Example output:

```text
media/intro.mp3       00:01:32.480
media/session.m4a     00:42:10.000

Files counted: 2
Total:         00:43:42.480
Average:       00:21:51.240
Longest:       media/session.m4a (00:42:10.000)
Shortest:      media/intro.mp3 (00:01:32.480)
Skipped:       3
```

## Implementation steps

1. Create a .NET console project with separate `Scanning`, `DurationParsing`,
   and `Reporting` namespaces.
2. Implement recursive discovery and extension-based candidate filtering.
3. Implement one parser at a time, starting with WAV and MP3, then MP4/M4A/MOV,
   FLAC, and OGG/Opus.
4. Normalize successful results to a single duration type and calculate total,
   average, shortest, and longest values.
5. Add clear terminal output and warnings for skipped files.
6. Add automated tests with `dotnet test`, using tiny, legal fixture files plus
   malformed-file cases.
7. Document `dotnet run`, `dotnet publish`, supported formats, limits, and
   exit-code behavior in the README.

## Acceptance criteria

- Running the command in a directory finds supported media files recursively.
- Each valid file has an accurate reported duration for its supported format.
- Summary totals, average, longest, and shortest are correct.
- Empty directories and directories containing only unsupported files produce a
  helpful zero-files summary without crashing.
- The .NET application builds and runs without FFmpeg, MediaInfo, or a
  comparable large multimedia dependency.
