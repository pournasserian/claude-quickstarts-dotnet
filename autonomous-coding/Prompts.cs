namespace AutonomousCoding;

/// <summary>Prompt loading utilities, ported from prompts.py.</summary>
public static class Prompts
{
    private static readonly string PromptsDir = Path.Combine(AppContext.BaseDirectory, "prompts");

    private static string LoadPrompt(string name) => File.ReadAllText(Path.Combine(PromptsDir, $"{name}.md"));

    public static string GetInitializerPrompt() => LoadPrompt("initializer_prompt");

    public static string GetCodingPrompt() => LoadPrompt("coding_prompt");

    /// <summary>Copies the app spec into the project directory for the agent to read.</summary>
    public static void CopySpecToProject(string projectDir)
    {
        var specSource = Path.Combine(PromptsDir, "app_spec.txt");
        var specDest = Path.Combine(projectDir, "app_spec.txt");
        if (!File.Exists(specDest))
        {
            File.Copy(specSource, specDest);
            Console.WriteLine("Copied app_spec.txt to project directory");
        }
    }
}
