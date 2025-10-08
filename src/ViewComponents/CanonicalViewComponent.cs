using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Webwonders.Baseline.Meta.Services;

namespace Webwonders.Baseline.Meta.ViewComponents;

[ViewComponent(Name = "HrefLang")]
public class HrefLangViewComponent(ILanguageService languageService) : ViewComponent
{
    public HtmlString Invoke(IPublishedContent model, string currentCulture)
    {
        var languages = languageService.GetLanguages(model, currentCulture, false);
        if (languages == null || languages.Languages.Count < 2)
        {
            return new HtmlString("");
        }

        var returnString = new StringBuilder();
        
        foreach (var language in languages.Languages)
        {
            returnString.AppendLine($"<link rel=\"alternate\" href=\"{language.Url}\" hreflang =\"{language.Culture}\" />");
        }

        return new HtmlString(returnString.ToString());
    }
}