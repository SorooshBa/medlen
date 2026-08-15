using Medlen.Scanning;

namespace Medlen.Reporting;

public static class ConsoleReporter
{
    public static void Write(string root, ScanResult scan)
    {
        Console.WriteLine($"Scanning: {root}");
        Console.WriteLine();

        if (scan.Files.Count == 0)
        {
            Console.WriteLine("No supported media files with readable durations were found.");
        }
        else
        {
            var width = Math.Min(72, scan.Files.Max(file => file.RelativePath.Length));
            foreach (var file in scan.Files)
                Console.WriteLine($"{Truncate(file.RelativePath, width).PadRight(width)}  {Format(file.Duration)}");

            var totalTicks = scan.Files.Aggregate(0L, (sum, file) => checked(sum + file.Duration.Ticks));
            var total = TimeSpan.FromTicks(totalTicks);
            var average = TimeSpan.FromTicks(totalTicks / scan.Files.Count);
            var longest = scan.Files.MaxBy(file => file.Duration)!;
            var shortest = scan.Files.MinBy(file => file.Duration)!;

            Console.WriteLine();
            Console.WriteLine($"Files counted: {scan.Files.Count}");
            Console.WriteLine($"Total:         {Format(total)}");
            Console.WriteLine($"Average:       {Format(average)}");
            Console.WriteLine($"Longest:       {longest.RelativePath} ({Format(longest.Duration)})");
            Console.WriteLine($"Shortest:      {shortest.RelativePath} ({Format(shortest.Duration)})");
        }

        Console.WriteLine($"Skipped:       {scan.Skipped.Count}");
        foreach (var skipped in scan.Skipped)
            Console.Error.WriteLine($"Warning: skipped {skipped.RelativePath}: {skipped.Reason}");
    }

    public static string Format(TimeSpan duration) =>
        $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}.{duration.Milliseconds:000}";

    private static string Truncate(string value, int width) =>
        value.Length <= width ? value : $"…{value[^Math.Max(1, width - 1)..]}";
}
