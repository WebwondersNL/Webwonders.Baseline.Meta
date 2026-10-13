using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Webwonders.Baseline.Meta.Models;
using Webwonders.Baseline.Meta.Options;
using static Webwonders.Baseline.Meta.Constants.LlmsTxt;

namespace Webwonders.Baseline.Meta.Controllers;

/// <summary>
/// Serves /llms.txt for the root node that matches the current domain.
/// Nothing is generated (404) when the root node has no llms.txt property, or when it is empty.
/// </summary>
[Route("llms.txt")]
[Route("{culture}/llms.txt")]
public class LlmsTxtController(
    ILogger<LlmsTxtController> logger,
    IDomainService domainService,
    IDocumentNavigationQueryService navigationQueryService,
    IUmbracoContextFactory umbracoContextFactory,
    IVariationContextAccessor variationContextAccessor,
    IOptions<WebwondersMetaSettings> settings) : Controller
{
    [HttpGet]
    public async Task<IActionResult> LlmsTxt(string? culture)
    {
        var aliasOverride = settings.Value.LlmsTxt.PropertyAlias;
        var compositionAlias = string.IsNullOrWhiteSpace(settings.Value.LlmsTxt.CompositionAlias)
            ? DefaultCompositionAlias
            : settings.Value.LlmsTxt.CompositionAlias;

        using var contextReference = umbracoContextFactory.EnsureUmbracoContext();
        var umbracoContext = contextReference.UmbracoContext;

        var currentUrl = umbracoContext.CleanedUmbracoUrl.AbsoluteUri;
        var requestUri = umbracoContext.CleanedUmbracoUrl;
        var requestPath = requestUri.AbsolutePath;
        if (requestPath.EndsWith("/llms.txt", StringComparison.OrdinalIgnoreCase))
            requestPath = requestPath[..^"/llms.txt".Length];
        requestPath = requestPath.Trim('/');

        var domains = (await domainService.GetAllAsync(false)).ToArray();
        var domain = FindDomain(domains, requestUri.Authority, requestPath);

        IPublishedContent? root = null;
        string? languageIsoCode = null;

        if (domain?.RootContentId != null)
        {
            // A matching domain decides the site. If that root has no LLMs.txt property the answer is a 404,
            // never another site's file.
            root = umbracoContext.Content?.GetById(domain.RootContentId.Value);
            languageIsoCode = domain.LanguageIsoCode;
        }
        else if (domains.Length == 0 && umbracoContext.Content != null && navigationQueryService.TryGetRootKeys(out var rootKeys))
        {
            // No domains configured at all (single-site setup): use the first root node with the property.
            root = rootKeys.Select(k => umbracoContext.Content.GetById(k)).FirstOrDefault(r => r != null && FindSettingsNode(r, compositionAlias) != null);
        }

        if (!string.IsNullOrWhiteSpace(languageIsoCode))
        {
            variationContextAccessor.VariationContext = new VariationContext(languageIsoCode);
        }

        // The property lives on the root itself or on its (Settings) child, whichever is composed of the composition.
        var settingsNode = root == null ? null : FindSettingsNode(root, compositionAlias);
        var alias = settingsNode == null ? null : FindAlias(settingsNode, aliasOverride);
        if (root == null || settingsNode == null || alias == null)
        {
            logger.LogInformation("LlmsTxtController: no root or child node with composition {composition} and an LLMs.txt property found for {url}", compositionAlias, currentUrl);
            return NotFound();
        }

        var raw = settingsNode.Value(alias);
        var json = raw switch
        {
            null => null,
            string s => s,
            _ => JsonSerializer.Serialize(raw)
        };

        LlmsTxtModel? model = null;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                model = JsonSerializer.Deserialize<LlmsTxtModel>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "LlmsTxtController: property {alias} on root {rootId} does not contain valid llms.txt JSON", alias, root.Id);
            }
        }

        if (model == null || (string.IsNullOrWhiteSpace(model.Title) && model.Chapters.Count == 0))
        {
            return NotFound();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(model.Title))
            sb.AppendLine($"# {model.Title}");

        if (!string.IsNullOrWhiteSpace(model.Description))
        {
            sb.AppendLine();
            sb.AppendLine($"> {model.Description}");
        }

        foreach (var chapter in model.Chapters)
        {
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(chapter.Title))
                sb.AppendLine($"## {chapter.Title}");

            sb.AppendLine();

            foreach (var item in chapter.Items)
            {
                var (name, rawUrl) = ResolveLink(umbracoContext, item);
                if (string.IsNullOrWhiteSpace(rawUrl)) continue;

                var url = Uri.IsWellFormedUriString(rawUrl, UriKind.Absolute) ? rawUrl : baseUrl + "/" + rawUrl.TrimStart('/');
                var line = $"- [{(string.IsNullOrWhiteSpace(name) ? url : name)}]({url})";

                if (!string.IsNullOrWhiteSpace(item.Description))
                    line += $": {item.Description}";

                sb.AppendLine(line);
            }
        }

        logger.LogDebug("LlmsTxtController: generated llms.txt for root {rootId}", root.Id);
        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }


    /// <summary>
    /// Finds the domain for a request by host (and optional path, e.g. "example.com/nl"), ignoring scheme.
    /// The longest matching path wins.
    /// </summary>
    private static IDomain? FindDomain(IDomain[] domains, string authority, string requestPath)
    {
        IDomain? best = null;
        var bestLength = -1;

        foreach (var d in domains)
        {
            var name = d.DomainName.Trim();
            var schemeIndex = name.IndexOf("://", StringComparison.Ordinal);
            if (schemeIndex >= 0) name = name[(schemeIndex + 3)..];

            var slash = name.IndexOf('/');
            var host = (slash >= 0 ? name[..slash] : name).Trim();
            var path = (slash >= 0 ? name[(slash + 1)..] : string.Empty).Trim('/');

            if (!string.Equals(host, authority, StringComparison.OrdinalIgnoreCase)) continue;

            var pathMatches = path.Length == 0
                || string.Equals(requestPath, path, StringComparison.OrdinalIgnoreCase)
                || requestPath.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase);
            if (!pathMatches) continue;

            if (path.Length > bestLength)
            {
                best = d;
                bestLength = path.Length;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns the root when it uses the composition, otherwise its first published child that does
    /// (Settings is an immediate child of the root), otherwise null.
    /// </summary>
    private static IPublishedContent? FindSettingsNode(IPublishedContent root, string compositionAlias) =>
        HasComposition(root, compositionAlias)
            ? root
            : root.Children().FirstOrDefault(c => HasComposition(c, compositionAlias));

    private static bool HasComposition(IPublishedContent node, string compositionAlias) =>
        node.ContentType.Alias.InvariantEquals(compositionAlias)
        || node.ContentType.CompositionAliases.Contains(compositionAlias, StringComparer.OrdinalIgnoreCase);

    /// <summary>Finds the LLMs.txt property on a node: by explicit alias when configured, otherwise by property editor.</summary>
    private static string? FindAlias(IPublishedContent node, string? aliasOverride) =>
        !string.IsNullOrWhiteSpace(aliasOverride)
            ? (node.HasProperty(aliasOverride) ? aliasOverride : null)
            : node.Properties.FirstOrDefault(p => p.PropertyType.EditorAlias == EditorAlias)?.Alias;

    /// <summary>Resolves an item to a display name and URL. Pages and media are looked up so the URL is always current.</summary>
    private static (string? Name, string? Url) ResolveLink(IUmbracoContext umbracoContext, LlmsTxtItem item)
    {
        var link = item.Link;
        if (link == null) return (item.Name, item.Url);

        string? url = link.Url;
        string? name = link.Name;

        if (link.Unique is { } key && link.Type is "document" or "media")
        {
            IPublishedContent? target = link.Type == "document"
                ? umbracoContext.Content?.GetById(key)
                : umbracoContext.Media?.GetById(key);

            // Unpublished or deleted targets are left out rather than emitting a dead link.
            if (target == null) return (null, null);

            url = target.Url(mode: UrlMode.Absolute);
            name = string.IsNullOrWhiteSpace(name) ? target.Name : name;
        }

        return (name, string.IsNullOrWhiteSpace(url) ? null : url + link.QueryString);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}
