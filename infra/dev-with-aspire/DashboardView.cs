// Copyright (c) Microsoft. All rights reserved.

namespace Aspire.AppHost;

internal static class DashboardView
{
    private const string Orchestrator = "orchestrator";
    private const string Qdrant = "qdrantstorage";
    private const string Redis = "redisstorage";
    private const string RedisCommander = Redis + "-commander";
    private const string RedisInsight = Redis + "-insight";
    private const string Postgres = "postgresstorage";
    private const string PgAdmin = Postgres + "-pgadmin";
    private const string Ollama = "ollama";
    private const string OllamaUi = Ollama + "-openwebui";

    public static void Configure(this IDistributedApplicationBuilder appBuilder, List<string> toolNames)
    {
        var builders = new Dictionary<string, IResourceBuilder<IResource>>();
        var resources = new Dictionary<string, IResource>();

        foreach (var resource in appBuilder.Resources)
        {
            resources[resource.Name] = resource;
            builders[resource.Name] = appBuilder.CreateResourceBuilder(resource);
        }

        ConfigureOrchestrator(resources, builders, appBuilder, toolNames);
        ConfigureQdrant(resources, builders);
        ConfigurePostgres(resources, builders);
        ConfigureRedis(resources, builders);
        ConfigureOllama(resources, builders);
    }

    /// <summary>
    /// Show all the tools under the orchestrator.
    /// For each tool, remove the endpoint and show the swagger link instead
    /// </summary>
    private static void ConfigureOrchestrator(
        Dictionary<string, IResource> resources,
        Dictionary<string, IResourceBuilder<IResource>> builders,
        IDistributedApplicationBuilder appBuilder,
        List<string> toolNames)
    {
        var tools = new HashSet<string>(toolNames);

        resources.TryGetValue(Orchestrator, out IResource? orchestrator);
        resources.TryGetValue(Postgres, out IResource? postgres);
        resources.TryGetValue(Redis, out IResource? redis);

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
        }
    }

    /// <summary>
    /// Organize Qdrant resources under the Qdrant resource and show links only on the root node.
    /// </summary>
    private static void ConfigureQdrant(
        Dictionary<string, IResource> resources,
        Dictionary<string, IResourceBuilder<IResource>> builders)
    {
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
    }

    /// <summary>
    /// Organize Postgres resources under the Postgres resource and show links only on the root node.
    /// </summary>
    private static void ConfigurePostgres(
        Dictionary<string, IResource> resources,
        Dictionary<string, IResourceBuilder<IResource>> builders)
    {
        resources.TryGetValue(Postgres, out IResource? postgres);
        resources.TryGetValue(PgAdmin, out IResource? pgAdmin);

        if (postgres != null && pgAdmin != null)
        {
            builders[PgAdmin].WithParentRelationship(postgres);
        }

        // Postgres pgAdmin link
        if (postgres != null && pgAdmin is IResourceWithEndpoints pgAdminWithEndpoints)
        {
            builders[Postgres]
                // Hide default endpoint
                .WithUrlForEndpoint("tcp", url => { url.Url = ""; })
                // Add a link to pgAdmin
                .WithUrls(pg =>
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
                });
        }
    }

    /// <summary>
    /// Organize Redis resources under the Redis resource and show links only on the root node.
    /// </summary>
    private static void ConfigureRedis(
        Dictionary<string, IResource> resources,
        Dictionary<string, IResourceBuilder<IResource>> builders)
    {
        resources.TryGetValue(Redis, out IResource? redis);
        resources.TryGetValue(RedisCommander, out IResource? redisCommander);
        resources.TryGetValue(RedisInsight, out IResource? redisInsight);

        if (redis != null && redisCommander != null)
        {
            builders[RedisCommander].WithParentRelationship(redis);
        }

        if (redis != null && redisInsight != null)
        {
            builders[RedisInsight].WithParentRelationship(redis);
        }

        // Redis links
        if (redis != null && redisCommander is IResourceWithEndpoints redisCommanderWithEndpoints)
        {
            builders[Redis]
                // Hide default endpoint
                .WithUrlForEndpoint("tcp", url => { url.Url = ""; })
                // Add a link to Commander
                .WithUrls(r =>
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
                });
        }

        if (redis != null && redisInsight is IResourceWithEndpoints redisInsightWithEndpoints)
        {
            builders[Redis]
                // Hide default endpoint
                .WithUrlForEndpoint("tcp", url => { url.Url = ""; })
                // Add a link to Commander
                .WithUrls(r =>
                {
                    // Add a link to Insight
                    r.Urls.Add(new ResourceUrlAnnotation
                    {
                        DisplayOrder = 200,
                        DisplayText = "Insight",
                        Url = redisInsightWithEndpoints.GetEndpoint("http").Url
                    });

                    // Hide Insight resource endpoint
                    redisInsightWithEndpoints.Annotations.Where(a => a is EndpointAnnotation).ToList().ForEach(a => redisInsightWithEndpoints.Annotations.Remove(a));
                });
        }
    }

    /// <summary>
    /// Organize Ollama resources under the Ollama resource and show links only on the root node.
    /// </summary>
    private static void ConfigureOllama(
        Dictionary<string, IResource> resources,
        Dictionary<string, IResourceBuilder<IResource>> builders)
    {
        resources.TryGetValue(Ollama, out IResource? ollama);
        resources.TryGetValue(OllamaUi, out IResource? ollamaUi);

        if (ollama != null && ollamaUi != null)
        {
            builders[OllamaUi].WithParentRelationship(ollama);
        }

        if (ollama != null && ollamaUi is IResourceWithEndpoints ollamaUiWithEndpoints)
        {
            builders[Ollama]
                // Hide default endpoint
                .WithUrlForEndpoint("http", url => { url.Url = ""; })
                // Add a link to Ollama Web UI
                .WithUrls(pg =>
                {
                    // Add a link to Ollama Web UI
                    pg.Urls.Add(new ResourceUrlAnnotation
                    {
                        DisplayOrder = 100,
                        DisplayText = "Web UI",
                        Url = ollamaUiWithEndpoints.GetEndpoint("http").Url
                    });

                    // Hide Ollama Web UI resource endpoint
                    ollamaUiWithEndpoints.Annotations.Where(a => a is EndpointAnnotation).ToList().ForEach(a => ollamaUiWithEndpoints.Annotations.Remove(a));
                });
        }
    }
}
