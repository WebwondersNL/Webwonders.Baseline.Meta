using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;
using Webwonders.Baseline.Meta.Options;

namespace Webwonders.Baseline.Meta.Controllers;

public class RobotsController(
    ILogger<RobotsController> logger,
    IDomainService domainService,
    IConfiguration configuration,
    ICompositeViewEngine compositeViewEngine,
    IOptions<WebwondersMetaSettings> settings
    ): UmbracoPageController(logger, compositeViewEngine)
{
    public async Task<IActionResult> Robots()
    {
        var domainObjects = await domainService.GetAllAsync(false);
        var domains = domainObjects.ToArray();
        
        var configSection = configuration.GetSection("Webwonders:Meta:Robots");

        List<string> rules = new List<string>();
        
        rules.Add(Constants.RobotsTxt.UserAgents.All);
        rules.Add($"{Constants.RobotsTxt.Content.Disallow}/umbraco/");

        var contentSignal = settings.Value.ContentSignal;
        if (contentSignal is { Enabled: true, AddToRobotsTxt: true })
        {
            rules.Add($"{Constants.RobotsTxt.Content.ContentSignal}{contentSignal.ToSignalValue()}");
        }

        var robotsSettings = settings.Value.Robots;
        if (robotsSettings.CrawlDelay > 0)
        {
            rules.Add($"{Constants.RobotsTxt.Content.CrawlDelay}{robotsSettings.CrawlDelay}");
        }

        if (robotsSettings.AllowAiCrawlers)
        {
            // A crawler with its own group ignores the "*" group, so the /umbraco/ block is repeated here.
            foreach (var crawler in robotsSettings.AiCrawlers.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                rules.Add(string.Empty);
                rules.Add($"User-agent: {crawler.Trim()}");
                rules.Add($"{Constants.RobotsTxt.Content.Allow}/");
                rules.Add($"{Constants.RobotsTxt.Content.Disallow}/umbraco/");
                if (robotsSettings.CrawlDelay > 0)
                {
                    rules.Add($"{Constants.RobotsTxt.Content.CrawlDelay}{robotsSettings.CrawlDelay}");
                }
            }
        }

        rules.Add(string.Empty);
        
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

        var excludedDomains = configSection.GetSection("ExcludedDomains").Get<string[]>() ?? Array.Empty<string>();

        foreach (var domain in domains)
        {
            if (string.IsNullOrWhiteSpace(domain.DomainName))
            {
                continue;
            }
                

            var domainHost = domain.DomainName.TrimEnd('/').ToLowerInvariant();
            if (excludedDomains.Any(e => domainHost.Contains(e.ToLowerInvariant())))
            {
                continue;
            }
            
            var fullUrl = domainHost.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? domainHost
                : $"https://{domainHost}";
            rules.Add($"Sitemap: {fullUrl.TrimEnd('/')}/sitemap.xml");
        }

        var generatedRobots = GenerateRobots(rules);
        return Content(generatedRobots, "text/plain");

    }

    private string GenerateRobots(List<string> rules)
    {
        return string.Join("\r\n", rules);
    }
}