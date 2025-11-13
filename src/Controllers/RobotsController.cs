using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace Webwonders.Baseline.Meta.Controllers;

public class RobotsController(
    ILogger<RobotsController> logger,
    IDomainService domainService,
    IUmbracoContextFactory umbracoContextFactory,
    IConfiguration configuration,
    ICompositeViewEngine compositeViewEngine
    ) : UmbracoPageController(logger, compositeViewEngine)
{
    public async Task<IActionResult> Robots()
    {
        var domainObjects = await domainService.GetAllAsync(false);
        var domains = domainObjects.ToArray();
        
        var configSection = configuration.GetSection("Webwonders:Meta:Robots");

        List<string> rules = new List<string>();
        
        rules.Add(Constants.RobotsTxt.UserAgents.All);
        rules.Add($"{Constants.RobotsTxt.Content.Disallow}/umbraco");
        
        if (configSection["CustomRobots"] != null && configSection["CustomRobots"] is { Length: > 0 })
        {
            var customRobotsValue = configSection["CustomRobots"];
            
            logger.LogInformation("RobotsController: Custom robots.txt found in configuration");
            if (customRobotsValue != null)
            {
                return Content(customRobotsValue);
            }
        }

        if (domains.Length == 0)
        {
            logger.LogError("RobotsController: domains not found");
            var generatedRules = GenerateRobots(rules);
            return Content(generatedRules, "text/plain");
        }

        var allDomains = domains
            .Select(d => d.DomainName)
            .Distinct()
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToList();

        if (allDomains.Count == 0)
        {
            logger.LogError("RobotsController: domains not found");
            var generatedRules = GenerateRobots(rules);
            return Content(generatedRules, "text/plain");
        }

        using (UmbracoContextReference umbracoContextReference = umbracoContextFactory.EnsureUmbracoContext())
        {
            foreach (var domain in domains)
            {
                if (domain.RootContentId.HasValue && !string.IsNullOrWhiteSpace(domain.LanguageIsoCode))
                {
                    var domainRootContent = umbracoContextReference?.UmbracoContext?.Content?.GetById(domain.RootContentId.Value);
                    var culture = domain.LanguageIsoCode;

                    if (domainRootContent != null && domainRootContent.IsPublished(culture))
                    {
                        bool isSitemapPublished = domainRootContent.Children(culture).Any(x =>
                            x.ContentType.Alias.Equals("sitemap") &&
                            x.IsPublished(culture));

                        if (isSitemapPublished)
                        {
                            string domainString = domain.DomainName;
                            var fullUrl = domainString.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? domainString
                                : $"https://{domainString}";
                            rules.Add($"Sitemap: {fullUrl.TrimEnd('/')}/sitemap.xml");
                        }
                    }
                }
            }
            
            var generatedRobots = GenerateRobots(rules);
            
            return Content(generatedRobots, "text/plain");
        }

    }

    private string GenerateRobots(List<string> rules)
    {
        return string.Join("\r\n", rules);
    }
}