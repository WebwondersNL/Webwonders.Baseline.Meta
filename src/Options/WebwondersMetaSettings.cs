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
}