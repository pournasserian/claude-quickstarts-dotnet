using System.Text.Json;

namespace AutonomousCoding;

/// <summary>Progress tracking utilities, ported from progress.py.</summary>
public static class Progress
{
    public static (int Passing, int Total) CountPassingTests(string projectDir)
    {
        var testsFile = Path.Combine(projectDir, "feature_list.json");
        if (!File.Exists(testsFile))
        {
            return (0, 0);
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(testsFile));
            var tests = doc.RootElement;
            if (tests.ValueKind != JsonValueKind.Array)
            {
                return (0, 0);
            }

            var total = tests.GetArrayLength();
            var passing = tests.EnumerateArray()
                .Count(t => t.TryGetProperty("passes", out var p) && p.ValueKind == JsonValueKind.True);

            return (passing, total);
        }
        catch (JsonException)
        {
            return (0, 0);
        }
        catch (IOException)
        {
            return (0, 0);
        }
    }

    public static void PrintSessionHeader(int sessionNum, bool isInitializer)
    {
        var sessionType = isInitializer ? "INITIALIZER" : "CODING AGENT";
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"  SESSION {sessionNum}: {sessionType}");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine();
    }

    public static void PrintProgressSummary(string projectDir)
    {
        var (passing, total) = CountPassingTests(projectDir);
        if (total > 0)
        {
            var percentage = (double)passing / total * 100;
            Console.WriteLine($"\nProgress: {passing}/{total} tests passing ({percentage:0.0}%)");
        }
        else
        {
            Console.WriteLine("\nProgress: feature_list.json not yet created");
        }
    }
}
