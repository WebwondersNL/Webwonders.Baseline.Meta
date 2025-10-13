namespace Webwonders.Baseline.Meta.Options;

public class WebwondersMetaSettings
{
    public const string ConfigurationName = "Webwonders:Meta";
    public string[] RedirectUrls { get; set; } = [];
    public string[] ExcludedDoctypesFromSitemaps { get; set; } = [];
}