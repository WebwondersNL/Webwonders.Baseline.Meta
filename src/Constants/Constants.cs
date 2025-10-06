namespace Webwonders.Baseline.Meta;

public static partial class Constants
{
    public static class RobotsTxt
    {
        
        public static class UserAgents
        {
            public const string All = "User-agent: *";
            public const string Google = "User-agent: Googlebot";
            public const string Bing = "User-agent: Bingbot";
            public const string Yahoo = "User-agent: Slurp";
            public const string DuckDuckGo = "User-agent: DuckDuckBot";
        }

        public static class Content
        {
            public const string Disallow = "Disallow: ";
            public const string Allow = "Allow: ";
            public const string Sitemap = "Sitemap: ";
        }
    }

    public static class Sitemap
    {
        public static class PageTypes
        {
            public const string Redirect = "redirect";
        }
        
        public static class Properties
        {
            public const string SearchEngineFrequency = "searchEngineChangeFrequency";
            public const string SearchEnginePriority = "searchEngineRelativePriority";
            public const string HideFromSearchEngines = "hideFromSearchEngines";
        }
        
        public static class Default
        {
            public const string ChangeFrequency = "monthly";
            public const double Priority = 0.5;
        }
    }
}