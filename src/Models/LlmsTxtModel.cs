namespace Webwonders.Baseline.Meta.Models;

/// <summary>The JSON value stored by the LLMs.txt property editor.</summary>
public class LlmsTxtModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<LlmsTxtChapter> Chapters { get; set; } = [];
}

public class LlmsTxtChapter
{
    public string? Title { get; set; }
    public List<LlmsTxtItem> Items { get; set; } = [];
}

public class LlmsTxtItem
{
    /// <summary>Link as produced by the Umbraco link picker (page, media or external).</summary>
    public LlmsTxtLink? Link { get; set; }
    public string? Description { get; set; }

    // Plain name/url pair, used by values saved before the link picker was introduced.
    public string? Name { get; set; }
    public string? Url { get; set; }
}

public class LlmsTxtLink
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public Guid? Unique { get; set; }
    /// <summary>"document", "media" or "external".</summary>
    public string? Type { get; set; }
    public string? QueryString { get; set; }
}
