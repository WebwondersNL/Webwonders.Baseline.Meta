using System.Globalization;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;
using Webwonders.Baseline.Meta.Models;
using Webwonders.Baseline.Meta.Options;

namespace Webwonders.Baseline.Meta.Services;

public interface ILanguageService
{
    LanguagesModel? GetLanguages(IPublishedContent currentPage, string? culture, bool fallbackToAncestors = true);
}

public class LanguageService(IOptions<WebwondersMetaSettings> settings) : ILanguageService
{

    public LanguagesModel? GetLanguages(IPublishedContent currentPage, string? culture, bool fallbackToAncestors = true)
    {
        if (culture.IsNullOrWhiteSpace())
        {
            return null;
        }

        var useRegionInName = settings.Value.UseRegionInLanguageName;

        var currentCulture = new CultureInfo(culture);
        var result = new LanguagesModel
        {
            CurrentName = GetDisplayName(currentCulture, useRegionInName),
            CurrentFlagClass = new RegionInfo(currentCulture.LCID).TwoLetterISORegionName.ToLower()
        };

        var home = currentPage.Root();
        var rootCultures = home?.Cultures.Select(x => x.Value);
        var pageCultures = currentPage.Cultures.Select(x => x.Value).ToArray();
        var showInLanguageMenuPropertyAlias = "showInLanguageMenu";

        if (home != null && rootCultures != null)
        {
            foreach (var rootCulture in rootCultures)
            {
                if (!home.Value<bool>(showInLanguageMenuPropertyAlias, rootCulture.Culture))
                    continue;

                var currentCultureInfo = new CultureInfo(rootCulture.Culture);
                var currentLanguageInfo = new RegionInfo(currentCultureInfo.LCID);
                
                var pageHasCulture = pageCultures.Any(x => x.Culture == rootCulture.Culture);
                
                var url = pageHasCulture
                    ? currentPage.Url(rootCulture.Culture, mode: UrlMode.Absolute)
                    : home.Url(rootCulture.Culture, mode: UrlMode.Absolute);
                
                if (!fallbackToAncestors && !pageHasCulture)
                    continue;

                var languageModel = new LanguageModel
                {
                    Name = GetDisplayName(currentCultureInfo, useRegionInName),
                    FlagClass = currentLanguageInfo.TwoLetterISORegionName.ToLower(),
                    Url = url,
                    Culture = currentCultureInfo.Name
                };

                result.Languages.Add(languageModel);
            }
        }

        return result;
    }

    private static string GetDisplayName(CultureInfo cultureInfo, bool useRegionInName) =>
        useRegionInName || cultureInfo.IsNeutralCulture
            ? cultureInfo.NativeName
            : cultureInfo.Parent.NativeName;
}