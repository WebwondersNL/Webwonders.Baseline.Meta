namespace Webwonders.Baseline.Meta.Models;

public class LanguageModel
{
    public string? Name { get; set; }
    public string? Culture { get; set; }
    public string? FlagClass { get; set; }
    public string? Url { get; set; }
}


public class LanguagesModel
{
    public string? CurrentName { get; set; }

    public string? CurrentFlagClass { get; set; }

    public List<LanguageModel> Languages { get; set; }

    public LanguagesModel()
    {
        Languages = new List<LanguageModel>();
    }
}