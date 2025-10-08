using System.Globalization;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Extensions;
using Webwonders.Baseline.Meta.Models;

namespace Webwonders.Baseline.Meta.Services;

public interface ILanguageService
{
    LanguagesModel? GetLanguages(IPublishedContent currentPage, string? culture, bool fallbackToAncestors = true);
}

public class LanguageService(IPublishedSnapshotAccessor publishedSnapshotAccessor) : ILanguageService
{
    private readonly IPublishedSnapshotAccessor _publishedSnapshotAccessor = publishedSnapshotAccessor;

    public LanguagesModel? GetLanguages(IPublishedContent currentPage, string? culture, bool fallbackToAncestors = true)
        {
            if (culture.IsNullOrWhiteSpace())
            {
                return null;
            }

            var currentCulture = new CultureInfo(culture);
            var result = new LanguagesModel()
            {
                CurrentName = currentCulture.IsNeutralCulture ? currentCulture.NativeName : currentCulture.Parent.NativeName,
                CurrentFlagClass = new RegionInfo(currentCulture.LCID).TwoLetterISORegionName.ToLower()
            };

            var home = currentPage.Root();
            var rootCultures = home?.Cultures.Select(x => x.Value);
            var pageCultures = currentPage.Cultures.Select(x => x.Value).ToArray();
            var showInLanguageMenuPropertyAlias = "showInLanguageMenu"; // We must set this manually as we don't have access to the Home model here
            if (home != null && rootCultures != null)
            {
                foreach (var rootCulture in rootCultures)
                {
                    if (home.Value<bool>(showInLanguageMenuPropertyAlias, rootCulture.Culture))
                    {
                        var currentCultureInfo = new CultureInfo(rootCulture.Culture);
                        var currentLanguageInfo = new RegionInfo(currentCultureInfo.LCID);

                        var pageExistsInLanguages = !fallbackToAncestors && pageCultures.Any(x => x.Culture == rootCulture.Culture);
                        
                        var languageModel = new LanguageModel()
                        {
                            Name = currentCultureInfo.IsNeutralCulture ? currentCultureInfo.NativeName : currentCultureInfo.Parent.NativeName,
                            FlagClass = currentLanguageInfo.TwoLetterISORegionName.ToLower(),
                            Url = pageExistsInLanguages
                                ? $"{currentPage.Url(rootCulture.Culture, mode: UrlMode.Absolute)}"
                                : $"{home.Url(rootCulture.Culture, mode: UrlMode.Absolute)}",
                            Culture = currentCultureInfo.Name
                        };
                        
                        if (fallbackToAncestors || pageExistsInLanguages)
                        {
                            result.Languages.Add(languageModel);
                        }
                    }
                }
            }

            return result;
        }
}