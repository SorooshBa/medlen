using Medlen.DurationParsing;

namespace Medlen.Scanning;

public sealed record MediaFile(string RelativePath, TimeSpan Duration);
public sealed record SkippedFile(string RelativePath, string Reason);
public sealed record ScanResult(IReadOnlyList<MediaFile> Files, IReadOnlyList<SkippedFile> Skipped);

public static class MediaScanner
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".flac", ".ogg", ".opus", ".mp4", ".m4a", ".mov"
    };

    public static ScanResult Scan(string root)
    {
        var files = new List<MediaFile>();
        var skipped = new List<SkippedFile>();
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            // EnumerateFiles already omits directory entries. Skipping the Directory
            // attribute here also prevents recursion into every subdirectory.
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        try
        {
            foreach (var path in Directory.EnumerateFiles(root, "*", options))
            {
                if (!SupportedExtensions.Contains(Path.GetExtension(path)))
                    continue;

                var relativePath = Path.GetRelativePath(root, path);
                if (MediaDurationReader.TryRead(path, out var duration, out var reason))
                    files.Add(new MediaFile(relativePath, duration));
                else
                    skipped.Add(new SkippedFile(relativePath, reason));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            skipped.Add(new SkippedFile("[directory scan]", exception.Message));
        }

        return new ScanResult(
            files.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray(),
            skipped.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
