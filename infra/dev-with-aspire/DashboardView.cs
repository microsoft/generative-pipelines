// Copyright (c) Microsoft. All rights reserved.

using Aspire.Hosting.Postgres;
using Aspire.Hosting.Redis;

namespace Aspire.AppHost;

internal static class DashboardView
{
    private const string Orchestrator = "orchestrator";
    private const string Qdrant = "qdrantstorage";
    private const string Redis = "redisstorage";
    private const string RedisCommander = "redisstorage-commander";
    private const string RedisInsights = "redisstorage-insight";
    private const string Postgres = "postgresstorage";
    private const string PgAdmin = "postgresstorage-pgadmin";

    public static void ConfigureDashboard(this IDistributedApplicationBuilder appBuilder, List<string> toolNames)
    {
        var tools = new HashSet<string>(toolNames);
        var builders = new Dictionary<string, IResourceBuilder<IResource>>();
        var resources = new Dictionary<string, IResource>();

        foreach (var resource in appBuilder.Resources)
        {
            resources[resource.Name] = resource;
            builders[resource.Name] = appBuilder.CreateResourceBuilder(resource);
        }

        resources.TryGetValue(Orchestrator, out IResource? orchestrator);
        resources.TryGetValue(Postgres, out IResource? postgres);
        resources.TryGetValue(PgAdmin, out IResource? pgAdmin);
        resources.TryGetValue(Redis, out IResource? redis);
        resources.TryGetValue(RedisCommander, out IResource? redisCommander);
        resources.TryGetValue(RedisInsights, out IResource? redisInsights);

        foreach (var resource in appBuilder.Resources)
        {
            var resBuilder = builders[resource.Name];

            if (tools.Contains(resource.Name))
            {
                // Link tools to orchestrator
                if (orchestrator != null) { resBuilder.WithParentRelationship(orchestrator); }

                // Remove links from tools
                resBuilder
                    .WithUrlForEndpoint("http", url =>
                    {
                        var path = (resource is UvicornAppResource or NodeAppResource) ? "/docs" : "/swagger";

                        url.DisplayOrder = 100;
                        url.DisplayText = "swagger";
                        url.Url = url.Url.TrimEnd('/') + path;
                    })
                    .WithUrlForEndpoint("https", url => { url.Url = ""; });
            }

            // Link Postgres tools to Postgres
            if (postgres != null && resource is PgAdminContainerResource or PgWebContainerResource)
            {
                resBuilder.WithParentRelationship(postgres);
            }

            // Link Redis tools to Redis
            if (redis != null && resource is RedisCommanderResource or RedisInsightResource)
            {
                resBuilder.WithParentRelationship(redis);
            }
        }

        // Qdrant dashboard, remove other URLs
        if (resources.ContainsKey(Qdrant))
        {
            builders[Qdrant]
                .WithUrlForEndpoint("grpc", url => { url.Url = ""; })
                .WithUrlForEndpoint("http", url =>
                {
                    url.DisplayOrder = 100;
                    url.DisplayText = "Dashboard";
                    url.Url = url.Url.TrimEnd('/') + "/dashboard";
                });
        }

        // Postgres pgAdmin link
        if (postgres != null && pgAdmin != null)
        {
            builders[Postgres]
                // Hide TCP endpoint
                .WithUrlForEndpoint("tcp", url => { url.Url = ""; })
                // Add a link to pgAdmin
                .WithUrls(pg =>
                {
                    if (pgAdmin is IResourceWithEndpoints pgAdminWithEndpoints)
                    {
                        // Add a link to pgAdmin
                        pg.Urls.Add(new ResourceUrlAnnotation
                        {
                            DisplayOrder = 100,
                            DisplayText = "pgAdmin",
                            Url = pgAdminWithEndpoints.GetEndpoint("http").Url
                        });

                        // Hide pgAdmin resource endpoint
                        pgAdminWithEndpoints.Annotations.Where(a => a is EndpointAnnotation).ToList().ForEach(a => pgAdminWithEndpoints.Annotations.Remove(a));
                    }
                });
        }

        // Redis links
        if (redis != null && redisCommander != null)
        {
            builders[Redis]
                // Hide TCP endpoint
                .WithUrlForEndpoint("tcp", url => { url.Url = ""; })
                // Add a link to Commander
                .WithUrls(r =>
                {
                    if (redisCommander is IResourceWithEndpoints redisCommanderWithEndpoints)
                    {
                        // Add a link to Commander
                        r.Urls.Add(new ResourceUrlAnnotation
                        {
                            DisplayOrder = 100,
                            DisplayText = "Commander",
                            Url = redisCommanderWithEndpoints.GetEndpoint("http").Url
                        });

                        // Hide Commander resource endpoint
                        redisCommanderWithEndpoints.Annotations.Where(a => a is EndpointAnnotation).ToList().ForEach(a => redisCommanderWithEndpoints.Annotations.Remove(a));
                    }

                    if (redisInsights is IResourceWithEndpoints redisInsightsWithEndpoints)
                    {
                        // Add a link to Insights
                        r.Urls.Add(new ResourceUrlAnnotation
                        {
                            DisplayOrder = 200,
                            DisplayText = "Insight",
                            Url = redisInsightsWithEndpoints.GetEndpoint("http").Url
                        });

                        // Hide Insights resource endpoint
                        redisInsightsWithEndpoints.Annotations.Where(a => a is EndpointAnnotation).ToList().ForEach(a => redisInsightsWithEndpoints.Annotations.Remove(a));
                    }
                });
        }
    }

    public static void AddToList(this List<string> list, List<string> targetList)
    {
        targetList.AddRange(list);
    }
}
