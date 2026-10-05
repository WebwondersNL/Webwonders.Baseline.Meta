namespace Webwonders.Baseline.Meta.Options;

public class WebwondersMetaSettings
{
    public const string ConfigurationName = "Webwonders:Meta";
    public string[] RedirectUrls { get; set; } = [];
    public string[] ExcludedDoctypesFromSitemaps { get; set; } = [];
    public string[] ExcludedDomainsFromSitemaps { get; set; } = [];

    /// <summary>
    /// When true, language menu names include the region (e.g. "Nederlands (België)") so sites with
    /// multiple regions per language stay distinguishable. Defaults to false (parent language name only).
    /// </summary>
    public bool UseRegionInLanguageName { get; set; }

    public ContentSignalSettings ContentSignal { get; set; } = new();

    public RobotsSettings Robots { get; set; } = new();
}

public class RobotsSettings
{
    /// <summary>When true, adds an explicit "Allow: /" group for each crawler in <see cref="AiCrawlers"/>.</summary>
    public bool AllowAiCrawlers { get; set; }

    public string[] AiCrawlers { get; set; } =
        ["GPTBot", "ChatGPT-User", "ClaudeBot", "Claude-Web", "PerplexityBot", "Google-Extended"];

    /// <summary>Crawl-delay in seconds, applied to "*" and the AI crawler groups. 0 disables it.</summary>
    public int CrawlDelay { get; set; }
}

/// <summary>
/// Advisory AI-use preferences (Content-Signal). Opt-in via <c>Webwonders:Meta:ContentSignal:Enabled</c>.
/// </summary>
public class ContentSignalSettings
{
    public bool Enabled { get; set; }

    /// <summary>AI model training, fine-tuning and dataset creation.</summary>
    public bool AiTrain { get; set; }

    /// <summary>AI search indexing, snippets and discovery.</summary>
    public bool Search { get; set; } = true;

    /// <summary>AI answer grounding, retrieval and generated-response context.</summary>
    public bool AiInput { get; set; } = true;

    /// <summary>Send Content-Signal and Content-Usage HTTP response headers on every response.</summary>
    public bool SendHeaders { get; set; } = true;

    /// <summary>Add the (experimental) Content-Signal directive to the generated robots.txt.</summary>
    public bool AddToRobotsTxt { get; set; } = true;

    public string ToSignalValue() =>
        $"ai-train={YesNo(AiTrain)}, search={YesNo(Search)}, ai-input={YesNo(AiInput)}";

    public string ToUsageValue() => $"train-ai={(AiTrain ? "y" : "n")}";

    private static string YesNo(bool value) => value ? "yes" : "no";
}