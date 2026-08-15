# Medlen

Medlen is a lightweight, cross-platform command-line tool that recursively
finds media files and reports their durations. It also calculates the total,
average, longest, and shortest durations.

Medlen is written in C#/.NET and reads container metadata directly. It does not
require FFmpeg, MediaInfo, or another large multimedia framework.

## Features

- Recursive directory scanning
- Per-file duration output
- Total and average duration
- Longest and shortest file detection
- Stable, sorted output
- Warnings for unreadable or malformed files
- No symbolic-link traversal by default

## Install on Ubuntu

```bash
curl -fsSL 'https://dl.cloudsmith.io/public/sorooshb/medlen/cfg/setup/bash.deb.sh' \
  | sudo env distro=ubuntu codename=any-version bash
sudo apt update
sudo apt install medlen
```

After installation:

```bash
medlen --help
```

To remove Medlen:

```bash
sudo apt remove medlen
```

## Supported formats

| Format | Extensions |
| --- | --- |
| WAV | `.wav` |
| MP3 | `.mp3` |
| FLAC | `.flac` |
| Ogg Vorbis / Opus | `.ogg`, `.opus` |
| MP4 family | `.mp4`, `.m4a`, `.mov` |

## Usage

```text
medlen [directory]
```

With no argument, Medlen scans the current working directory. Pass a directory
to scan another location:

```bash
medlen /path/to/media
```

Use `-h` or `--help` to display usage information.

## Example output

```text
Scanning: /media/course

intro.mp4                    00:02:54.847
lessons/setup.mp4            00:03:10.473
lessons/final-project.mp4    00:08:17.743

Files counted: 3
Total:         00:14:23.063
Average:       00:04:47.687
Longest:       lessons/final-project.mp4 (00:08:17.743)
Shortest:      intro.mp4 (00:02:54.847)
Skipped:       0
```

## Build from source

Requirements:

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later

Run from the repository:

```bash
dotnet run
dotnet run -- /path/to/media
```

Build a release binary:

```bash
dotnet build --configuration Release
```

Publish a self-contained binary for Linux:

```bash
dotnet publish --configuration Release \
  --runtime linux-x64 --self-contained true
```

## Debian package development

Build a local 64-bit Debian package:

```bash
chmod +x packaging/debian/build-deb.sh
./packaging/debian/build-deb.sh 0.1.0
sudo apt install ./artifacts/deb/medlen_0.1.0_amd64.deb
```

Release publishing instructions for maintainers are in
[docs/PUBLISHING.md](docs/PUBLISHING.md).

## Limitations

Duration is read from format-specific metadata. Corrupt files, unusual
container variants, DRM-protected files, and some variable-bit-rate MP3 files
without Xing/Info metadata may not produce an exact duration. Unsupported files
are skipped and do not affect the summary.

## License

No license file is included yet. Add a license before distributing modified
versions of the project.
