using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Webwonders.Baseline.Meta.Controllers;

[Route("sitemap.xml")]
[Route("{culture}/sitemap.xml")]
public class SitemapController : Controller
{
    private readonly ILogger<SitemapController> _logger;
    private readonly IDomainService _domainService;
    private readonly IConfiguration _configuration;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ILanguageService _languageService;
    
    public SitemapController(
        ILogger<SitemapController> logger,
        IDomainService domainService,
        IConfiguration configuration,
        IVariationContextAccessor variationContextAccessor,
        IUmbracoContextFactory umbracoContextFactory,
        ILanguageService languageService
    )
    {
        _logger = logger;
        _domainService = domainService;
        _configuration = configuration;
        _variationContextAccessor = variationContextAccessor;
        _umbracoContextFactory = umbracoContextFactory;
        _languageService = languageService;
    }
    
    
    [HttpGet]
    public async Task<IActionResult> Sitemap(string? culture)
    {
        var configSection = _configuration.GetSection("Webwonders:Meta:Sitemap");
        
        var allDomains = _domainService.GetAllAsync(false).Result.ToArray();
        if (allDomains.Length <= 0)
        {
            _logger.LogError("SitemapController: No domains found for sitemap generation.");
            return NotFound();
        }
        
        var umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var umbracoContext = umbracoContextReference.UmbracoContext;
        var currentUrl = umbracoContext.CleanedUmbracoUrl.AbsoluteUri;
        var currentUrlWithoutSitemap = currentUrl.EndsWith("/sitemap.xml")
            ? currentUrl[..^"/sitemap.xml".Length]
            : currentUrl;
        

        var domain = allDomains.FirstOrDefault(d => currentUrlWithoutSitemap == d.DomainName.TrimEnd("/").ToLowerInvariant());

        if (domain == null)
        {
            _logger.LogWarning("SitemapController: Domain not found for Url {url}, falling back to the first domain we find.", currentUrlWithoutSitemap);
            
            //If we cant find a domain for the current Url, we should fall back to the first root domain we can find
            domain = allDomains.FirstOrDefault();
            
            if (domain == null)
            {
                // This means we cant find any domain at all, so we log an error and return NotFound
                _logger.LogWarning("SitemapController: No domain found at all, Something has probably gone wrong.");
                return NotFound();
            }
        }
        
        // Get the default language from Umbraco if domain.LanguageIsoCode is empty or null

        var languageIsoCode = !string.IsNullOrWhiteSpace(domain.LanguageIsoCode)
            ? domain.LanguageIsoCode
            : await _languageService.GetDefaultIsoCodeAsync();
                

        var rootContentId = domain.RootContentId;
        var root = umbracoContext.Content?.GetById(rootContentId ?? 0);

        if (root == null)
        {
            _logger.LogWarning(
                $"SitemapController: Root content not found for domain {domain.LanguageIsoCode} with RootContentId {rootContentId}."
            );

            var firstDomain = allDomains.FirstOrDefault();

            if (firstDomain != null)
            {
                root = umbracoContext.Content?.GetById(firstDomain.RootContentId ?? 0);
                languageIsoCode = firstDomain.LanguageIsoCode;
            }
        }
        
        var excludedDoctypes = configSection.GetSection("ExcludedDoctypesFromSitemaps").Get<string[]>() ?? Array.Empty<string>();
        
        // Build the XML
        var sb = new StringBuilder();
        
        if (root != null && languageIsoCode != null)
        {
            sb.Append(RenderSiteMapUrlEntry(root, languageIsoCode));
            sb.Append(RenderSiteMapUrlEntriesForChildren(root, excludedDoctypes.ToList(), languageIsoCode));
        }

        if (sb.Length == 0)
            return Content(string.Empty, "application/xml");

        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                  $"<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n" +
                  sb +
                  $"</urlset>";

        return Content(xml, "application/xml");
    }

    public string RenderSiteMapUrlEntry(IPublishedContent node, string languageIsoCode)
    {
        _variationContextAccessor.VariationContext = new VariationContext(languageIsoCode);
        
        var changeFrequency = node.HasValue(Constants.Sitemap.Properties.SearchEngineFrequency, fallback: Fallback.ToAncestors)
            ? node.Value(Constants.Sitemap.Properties.SearchEngineFrequency, fallback: Fallback.ToAncestors)
            : Constants.Sitemap.Default.ChangeFrequency;
        
        var priority = node.HasValue(Constants.Sitemap.Properties.SearchEnginePriority, fallback: Fallback.ToAncestors)
            ? node.Value(Constants.Sitemap.Properties.SearchEnginePriority)
            : Constants.Sitemap.Default.Priority;

        var renderNode = true;
        var url = node.Url(mode: UrlMode.Absolute);

        if (node.ContentType.Alias == Constants.Sitemap.PageTypes.Redirect)
        {
            var redirectTo = node.Value<Link>(Constants.Sitemap.Properties.RedirectToProperty);

            if (redirectTo != null && (redirectTo.Url == null || 
                                       (redirectTo.Type == LinkType.External && !redirectTo.Url.Contains(Request.Host.Host))))
            {
                renderNode = false;
            }
            else
            {
                url = redirectTo?.Url;
            }
        }

        if (renderNode)
        {
            var stringBuilderItem = new StringBuilder();
            
            stringBuilderItem.AppendLine("<url>");
            stringBuilderItem.AppendLine($"<loc>{url}</loc>");
            stringBuilderItem.AppendLine($"<changefreq>{changeFrequency}</changefreq>");
            stringBuilderItem.AppendLine($"<priority>{priority}</priority>");
            stringBuilderItem.AppendLine("</url>");
            
            return stringBuilderItem.ToString();
        }
        
        return string.Empty;
    }

    private string RenderSiteMapUrlEntriesForChildren(IPublishedContent parentPage, List<string> excludedDocumentTypes,
        string languageIsoCode)
    {
        _variationContextAccessor.VariationContext = new VariationContext(languageIsoCode);
        
        var stringBuilder = new StringBuilder();
        
        foreach(var page in parentPage?.Children()?.Where(f => !excludedDocumentTypes.Contains(f.ContentType.Alias) && !f.Value<bool>(Constants.Sitemap.Properties.HideFromSearchEngines))!)
        {
            stringBuilder.Append(RenderSiteMapUrlEntry(page, languageIsoCode));
            
            if((bool)page?.Children()?.Any(f => !excludedDocumentTypes.Contains(f.ContentType.Alias) && !f.Value<bool>(Constants.Sitemap.Properties.HideFromSearchEngines))!)
            {
                stringBuilder.Append(RenderSiteMapUrlEntriesForChildren(page, excludedDocumentTypes, languageIsoCode));
            }
        }

        return stringBuilder.ToString();
    }
}