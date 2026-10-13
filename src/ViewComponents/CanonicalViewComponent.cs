using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Webwonders.Baseline.Meta.Services;
using UmbracoLanguageService = Umbraco.Cms.Core.Services.ILanguageService;

namespace Webwonders.Baseline.Meta.ViewComponents;

[ViewComponent(Name = "HrefLang")]
public class HrefLangViewComponent(ILanguageService languageService, UmbracoLanguageService umbracoLanguageService) : ViewComponent
{
    public async Task<HtmlString> InvokeAsync(IPublishedContent model, string currentCulture)
    {
        var languages = languageService.GetLanguages(model, currentCulture, false);
        if (languages == null || languages.Languages.Count < 2)
        {
            return new HtmlString("");
        }

        var returnString = new StringBuilder();

        foreach (var language in languages.Languages)
        {
            returnString.AppendLine($"<link rel=\"alternate\" href=\"{language.Url}\" hreflang=\"{language.Culture}\" />");
        }

        // x-default points to the site's default language, if that language is one of the alternates
        var defaultIsoCode = await umbracoLanguageService.GetDefaultIsoCodeAsync();
        var defaultLanguage = languages.Languages.FirstOrDefault(x =>
            string.Equals(x.Culture, defaultIsoCode, StringComparison.OrdinalIgnoreCase));
        if (defaultLanguage != null)
        {
            returnString.AppendLine($"<link rel=\"alternate\" href=\"{defaultLanguage.Url}\" hreflang=\"x-default\" />");
        }

        return new HtmlString(returnString.ToString());
    }
}
