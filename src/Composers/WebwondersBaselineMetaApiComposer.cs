using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Webwonders.Baseline.Meta.Controllers;

namespace Webwonders.Baseline.Meta.Composers
{
    public class WebwondersBaselineMetaComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.Configure<UmbracoPipelineOptions>(options =>
            {
                options.AddFilter(new UmbracoPipelineFilter(nameof(RobotsController))
                {
                    Endpoints = app => app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllerRoute(
                            "Robots Controller",
                            "/robots.txt",
                            new { Controller = "Robots", Action = "Robots" });
                    })
                });
            });
            
            builder.Services.Configure<UmbracoPipelineOptions>(options =>
            {
                options.AddFilter(new UmbracoPipelineFilter(nameof(SitemapController))
                {
                    Endpoints = app => app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllerRoute(
                            "Sitemap Controller",
                            "/sitemap.xml",
                            new { Controller = "Sitemap", Action = "Sitemap" });

                        endpoints.MapControllerRoute(
                            "Sitemap Controller with culture",
                            "/{culture}/sitemap.xml",
                            new { Controller = "Sitemap", Action = "Sitemap" }
                        );
                    })
                });
            });
        }
    }
}
