using Medlen.Reporting;
using Medlen.Scanning;

if (args.Length > 1 || args.FirstOrDefault() is "-h" or "--help")
{
    Console.WriteLine("Usage: medlen [directory]");
    Console.WriteLine("Recursively reports durations for WAV, MP3, FLAC, OGG/Opus, MP4, M4A, and MOV files.");
    return args.Length > 1 ? 2 : 0;
}

var root = Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Error: directory not found: {root}");
    return 2;
}

var scan = MediaScanner.Scan(root);
ConsoleReporter.Write(root, scan);
return 0;
