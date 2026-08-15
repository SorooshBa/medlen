# Medlen

Medlen is a lightweight, cross-platform command-line tool that recursively
finds media files and reports their durations. It also calculates the total and
average duration, plus the longest and shortest files.

It is written in C#/.NET and uses no FFmpeg, MediaInfo, or native multimedia
frameworks.

## Features

- Recursively scans a directory (the current directory by default)
- Reports the duration of each supported media file
- Calculates total and average duration
- Identifies the longest and shortest files
- Sorts paths for repeatable output
- Skips unreadable or unsupported files with a clear warning
- Follows no symbolic links by default

## Supported formats

| Format | Extensions | Duration source |
| --- | --- | --- |
| WAV | `.wav` | RIFF audio metadata |
| MP3 | `.mp3` | MPEG frame headers and Xing/Info metadata |
| FLAC | `.flac` | STREAMINFO metadata |
| Ogg Vorbis / Opus | `.ogg`, `.opus` | Stream header and final granule position |
| MP4 family | `.mp4`, `.m4a`, `.mov` | MP4 `mvhd` movie metadata |

## Requirements

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later

## Run from source

Clone the repository, then run Medlen in the current directory:

```bash
dotnet run
```

To scan a specific directory:

```bash
dotnet run -- "/path/to/media"
```

For example:

```bash
dotnet run -- "/mnt/e/Courses/My Course"
```

## Build a standalone executable

Build the project:

```bash
dotnet build --configuration Release
```

Or publish a self-contained executable for your platform. Replace the runtime
identifier as needed (`win-x64`, `linux-x64`, `osx-arm64`, and so on):

```bash
dotnet publish --configuration Release --runtime linux-x64 --self-contained true
```

The published executable is placed under:

```text
bin/Release/net10.0/linux-x64/publish/
```

## Install on Debian and Ubuntu

Medlen is prepared as a self-contained `amd64` Debian package, so installed
users do not need the .NET runtime. It targets current 64-bit Debian/Ubuntu
systems with `libssl3` (for example, Debian 12+ and Ubuntu 22.04+). Build the
package on a Debian/Ubuntu machine with the .NET 10 SDK and `dpkg-deb`
available:

```bash
chmod +x packaging/debian/build-deb.sh
./packaging/debian/build-deb.sh 0.1.0
```

This creates `artifacts/deb/medlen_0.1.0_amd64.deb`. Install a locally built or
downloaded release package with:

```bash
sudo apt install ./artifacts/deb/medlen_0.1.0_amd64.deb
```

After the project is published to Cloudsmith, users add its signed repository
source once and then install or upgrade Medlen normally:

```bash
curl -1sLf 'https://dl.cloudsmith.io/public/sorooshb/medlen/cfg/setup/bash.deb.sh' | sudo -E bash
sudo apt update
sudo apt install medlen
```

The GitHub Actions workflow builds a `.deb` artifact for each release tag, such
as `v0.1.0`, and uploads it to Cloudsmith. See
[the publishing guide](docs/PUBLISHING.md) for the one-time account and GitHub
secret setup.

### Publishing to an APT repository

Cloudsmith hosts and signs the APT repository. Set the `CLOUDSMITH_API_KEY`
GitHub secret and `CLOUDSMITH_REPOSITORY` GitHub Actions variable to
`sorooshb/medlen`, then publish a tag. Detailed instructions are in
[the publishing guide](docs/PUBLISHING.md).

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

## Command reference

```text
medlen [directory]
```

| Argument | Description |
| --- | --- |
| `directory` | Optional directory to scan. Defaults to the current working directory. |
| `-h`, `--help` | Print usage information. |

Medlen returns exit code `2` when the command is invalid or the target directory
does not exist. An unreadable or malformed individual media file produces a
warning but does not stop the scan.

## Limitations

Media duration lives in container-specific metadata, and Medlen deliberately
avoids large multimedia libraries. Therefore, it supports the common formats
listed above rather than every possible media container or codec. Corrupted
files, unusual container variants, DRM-protected files, and some variable-bit-rate
MP3 files without Xing/Info metadata may not yield an exact duration.

## Development

Build and verify the project with:

```bash
dotnet build
```

## License

Add a license file before publishing if you want to grant others explicit reuse
rights.
