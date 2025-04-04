// Copyright (c) Microsoft. All rights reserved.

using Aspire.AppHost.Internals;
using Aspire.AppHost.PatchedAddUvicornApp;
using Aspire.Hosting.Azure;
using Azure.Core;
using Azure.Provisioning.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.AppHost;

#pragma warning disable ASPIREHOSTINGPYTHON001
internal static class Program
{
    private const string ToolNameEnvVar = "TOOL_NAME";
    private const string RedisConnStringName = "redis-storage";
    private const string QdrantConnStringName = "qdrant-storage";
    private const string PostgresConnStringName = "postgres-storage";
    private const string AzureAiSearchConnStringName = "aisearch-storage";
    private const string AzureBlobsConnStringName = "blobs-storage";
    private const string AzureQueuesConnStringName = "queues-storage";

    // Let the containers run when the host stops. Used for Qdrant, Redis, etc.
    private const ContainerLifetime ExternalContainersLifetime = ContainerLifetime.Persistent;

    // Directory containing the tools to be available via the orchestrator.
    private static readonly string s_toolsPath = Path.Join("..", "..", "tools");

    // Use path rather than a reference, to decouple nuget dependencies management
    private static readonly string s_orchestratorProjectFile = Path.Join("..", "service", "Orchestrator", "Orchestrator.csproj");

    // Where local docker image will persist their data across multiple executions, e.g. Redis cache, Postgres DBs, Qdrant collections, etc.
    // If you change this path, make sure all resources can write to it. Don't use Program.cs to create the dir to avoid incorrect permissions.
    private static readonly string s_localDockerData = Path.GetFullPath(Path.Join("..", "..", "tools", "_docker_data"));

    // Defined as static simply to avoid passing it around to all methods
    private static IDistributedApplicationBuilder s_builder = null!;

    private static AppSettings s_config = new();

    /// <summary>
    /// Discover and start all tools, together with the orchestrator and other resources.
    /// </summary>
    public static void Main(string[] args)
    {
        if (!Directory.Exists(s_toolsPath))
        {
            Console.Error.WriteLine($"Error: Tools directory not found at: {s_toolsPath}");
            Environment.Exit(1);
        }

        s_builder = DistributedApplication.CreateBuilder(args);
        s_config = s_builder.Configuration.GetSection("App").Get<AppSettings>() ?? new();

        CreateDockerDataBindMountDirectory();

        // Tip: see https://azure.microsoft.com/products/storage/storage-explorer to browse Azure Storage
        var azureStorage = AddAzureStorage(); // Blobs and Queues
        var redis = AddRedisResources(); // Cache and KV
        var postgres = AddPostgresResources(); // Vector storage
        var qdrant = AddQdrantResources(); // Vector storage
        var azSearch = AddAzureAiSearch(); // Vector storage with semantic ranking
        var orchestrator = AddOrchestrator(azureStorage.blobs, azureStorage.queues, redis); // Orchestrator

        List<IResourceBuilder<IResourceWithConnectionString>?> references = [redis, azSearch, postgres, qdrant, azureStorage.blobs, azureStorage.queues];

        // Tools
        Echo("=================================");
        AddDotNetTools(orchestrator, references);
        AddNodeJsTools(orchestrator, references);
        AddPythonTools(orchestrator, references);
        Echo("=================================");

        s_builder.ShowDashboardUrl(true).Build().Run();
    }

    private static void CreateDockerDataBindMountDirectory()
    {
        if (s_builder.ExecutionContext.IsPublishMode) { return; }

        Echo($"Docker data bind mount: {s_localDockerData}");

        // Note: the directory must be created by docker to have the correct permissions otherwise
        // the container instance will report errors and run in a failed mode, breaking other resources.
        if (!Directory.Exists(s_localDockerData))
        {
            Console.WriteLine($"Error: Docker data directory not found at: {s_localDockerData}. Failed to create dir.");
        }
    }

    private static IResourceBuilder<ProjectResource> AddOrchestrator(
        IResourceBuilder<AzureBlobStorageResource> blobs,
        IResourceBuilder<AzureQueueStorageResource> queues,
        IResourceBuilder<IResourceWithConnectionString>? redis)
    {
        IResourceBuilder<ProjectResource> orchestrator = AddDotNetService("orchestrator", s_orchestratorProjectFile)
            .WithExternalHttpEndpoints(); // The orchestrator is the only public endpoint

        orchestrator.WithReference(blobs).WaitFor(blobs);
        orchestrator.WithReference(queues).WaitFor(queues);

        if (redis != null)
        {
            orchestrator.WithReference(redis).WaitFor(redis);
        }

        return orchestrator;
    }

    private static (IResourceBuilder<AzureBlobStorageResource> blobs, IResourceBuilder<AzureQueueStorageResource> queues) AddAzureStorage()
    {
        IResourceBuilder<AzureStorageResource> storage = s_builder.AddAzureStorage("AzureStorage");
        if (s_builder.Environment.IsDevelopment())
        {
            // Use Azurite
            storage.RunAsEmulator();
        }

        IResourceBuilder<AzureBlobStorageResource> blobs = storage.AddBlobs(AzureBlobsConnStringName);
        IResourceBuilder<AzureQueueStorageResource> queues = storage.AddQueues(AzureQueuesConnStringName);

        return (blobs, queues);
    }

    private static IResourceBuilder<IResourceWithConnectionString>? AddRedisResources()
    {
        if (!s_config.UseRedis) { return null; }

        // https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-integration
        IResourceBuilder<IResourceWithConnectionString> redis;

        // Use container on localhost, Azure Cache in the real deployment
        if (s_builder.ExecutionContext.IsPublishMode)
        {
            redis = s_builder.AddAzureRedis(RedisConnStringName);
        }
        else
        {
            if (s_config.UseRedisTools)
            {
                redis = s_builder.AddRedis(RedisConnStringName)
                    .WithLifetime(ExternalContainersLifetime)
                    .WithPersistence(interval: TimeSpan.FromSeconds(45), keysChangedThreshold: 1)
                    .WithDataBindMount(Path.Join(s_localDockerData, "redis"))
                    .WithRedisCommander()
                    .WithRedisInsight(x => x.WithLifetime(ExternalContainersLifetime));
            }
            else
            {
                redis = s_builder.AddRedis(RedisConnStringName)
                    .WithLifetime(ExternalContainersLifetime)
                    .WithPersistence(interval: TimeSpan.FromSeconds(45), keysChangedThreshold: 1)
                    .WithDataBindMount(Path.Join(s_localDockerData, "redis"));
            }
        }

        return redis;
    }

    private static IResourceBuilder<IResourceWithConnectionString>? AddAzureAiSearch()
    {
        // When running locally inject the connection string from appsettings
        if (s_builder.ExecutionContext.IsRunMode)
        {
            // Check if the connections string is set
            if (string.IsNullOrEmpty(s_builder.Configuration.GetConnectionString(AzureAiSearchConnStringName)))
            {
                Console.Error.WriteLine($"Error: Azure AI Search connection string not set in appsettings.json, skipping the resource.");
                return null;
            }

            return s_builder.AddConnectionString(AzureAiSearchConnStringName);
        }

        // When running in Azure, create the resource
        var bicepId = AzureAiSearchConnStringName.Replace("-", "_", StringComparison.Ordinal);
        return s_builder
            .AddAzureSearch(AzureAiSearchConnStringName)
            .ConfigureInfrastructure(infra =>
            {
                SearchService? res = infra.GetProvisionableResources().OfType<SearchService>().FirstOrDefault(x => x.BicepIdentifier == bicepId);

                if (res == null)
                {
                    Console.WriteLine("Search services:");
                    foreach (SearchService x in infra.GetProvisionableResources().OfType<SearchService>())
                    {
                        Console.WriteLine($" - '{x.BicepIdentifier}'");
                    }

                    throw new InvalidOperationException($"Azure Search resource '{AzureAiSearchConnStringName}' not found.");
                }

                res.SearchSkuName = SearchServiceSkuName.Standard; // Free, Basic, Standard ...
                res.SemanticSearch = SearchSemanticSearch.Disabled;
                res.IsLocalAuthDisabled = true; // Disable API key auth

                // Mar 2025: WestUS3 is not available
                if (res.Location.Value == AzureLocation.WestUS3)
                {
                    res.Location = AzureLocation.WestUS2;
                }
            });
    }

    private static IResourceBuilder<QdrantServerResource>? AddQdrantResources()
    {
        if (!s_config.UseQdrant) { return null; }

        const string ResourceName = "qdrant";
        IResourceBuilder<QdrantServerResource>? qdrant = null;

        // On Azure
        if (s_builder.ExecutionContext.IsPublishMode)
        {
            qdrant = s_builder.AddQdrant(QdrantConnStringName)
                .WithDataBindMount(Path.Join(s_localDockerData, ResourceName))
                .WithLifetime(ExternalContainersLifetime);
        }

        // On localhost
        if (s_builder.ExecutionContext.IsRunMode)
        {
            // var customSecret = s_builder.AddParameter($"{ResourceName}-Key", "value here");
            // qdrant = s_builder.AddQdrant(QdrantConnStringName, apiKey: customSecret)

            qdrant = s_builder.AddQdrant(QdrantConnStringName)
                .WithDataBindMount(Path.Join(s_localDockerData, ResourceName))
                .WithLifetime(ExternalContainersLifetime);
        }

        return qdrant;
    }

    private static IResourceBuilder<IResourceWithConnectionString>? AddPostgresResources()
    {
        if (!s_config.UsePostgres) { return null; }

        const string ResourceName = "postgres";
        IResourceBuilder<IResourceWithConnectionString>? postgres = null;

        // On localhost ...
        if (s_builder.ExecutionContext.IsRunMode)
        {
            // var customSecret = s_builder.AddParameter($"{ResourceName}-password", "value here");
            // s_builder.AddPostgres(PostgresConnStringName, password: customSecret)

            postgres = s_builder.AddPostgres(PostgresConnStringName)
                .WithImage(image: s_config.PostgresContainerImage, tag: s_config.PostgresContainerImageTag)
                .WithDataBindMount(Path.Join(s_localDockerData, ResourceName))
                .WithLifetime(ExternalContainersLifetime);
        }

        // On Azure ...
        if (s_builder.ExecutionContext.IsPublishMode && s_config.UsePostgresOnAzure)
        {
            postgres = s_builder.AddAzurePostgresFlexibleServer(PostgresConnStringName);
        }

        return postgres;
    }

    /// <summary>
    /// Discover and add all .NET tools.
    /// </summary>
    private static void AddDotNetTools(IResourceBuilder<ProjectResource> orchestrator,
        ICollection<IResourceBuilder<IResourceWithConnectionString>?> references)
    {
        try
        {
            var dnProjects = ToolDiscovery.FindDotNetProjects(s_toolsPath);
            foreach (var p in dnProjects)
            {
                Echo($"- Adding {p.name} (.NET {p.projectFilePath})");

                var resource = AddDotNetService(p.name, p.projectFilePath)
                    .WithEnvironment(ToolNameEnvVar, p.name);

                // Inject connection strings
                foreach (var r in references.Where(x => x != null)) { resource.WithReference(r!); }

                orchestrator.WithReference(resource);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering .NET projects: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Discover and add all Python tools.
    /// </summary>
    private static void AddPythonTools(IResourceBuilder<ProjectResource> orchestrator,
        ICollection<IResourceBuilder<IResourceWithConnectionString>?> references)
    {
        try
        {
            var tsProjects = ToolDiscovery.FindPythonProjects(s_toolsPath);
            foreach (var p in tsProjects)
            {
                Echo($"- Adding {p.name} ({p.dirName} Python tool)");

                var resource = AddPythonService(p.name, p.dirName)
                    .WithEnvironment(ToolNameEnvVar, p.name);

                // Inject connection strings
                foreach (var r in references.Where(x => x != null)) { resource.WithReference(r!); }

                orchestrator.WithReference(resource);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Python projects: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Discover and add all Node.js tools.
    /// </summary>
    private static void AddNodeJsTools(IResourceBuilder<ProjectResource> orchestrator,
        ICollection<IResourceBuilder<IResourceWithConnectionString>?> references)
    {
        try
        {
            var tsProjects = ToolDiscovery.FindNodeJsProjects(s_toolsPath);
            foreach (var p in tsProjects)
            {
                Echo($"- Adding {p.name} ({p.dirName} Node.js tool)");

                var resource = AddNodeJsService(p.name, p.dirName)
                    .WithEnvironment(ToolNameEnvVar, p.name);

                // Inject connection strings
                foreach (var r in references.Where(x => x != null)) { resource.WithReference(r!); }

                orchestrator.WithReference(resource);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Node.js projects: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Add a .NET service.
    /// Important: .NET projects must have their launchSettings defining the http and https endpoints
    /// </summary>
    /// <param name="name">Service name</param>
    /// <param name="projectFile">Path to the .csproj file</param>
    private static IResourceBuilder<ProjectResource> AddDotNetService(
        string name,
        string projectFile)
    {
        var p = s_builder.AddProject(name: name, projectPath: Path.Join(s_toolsPath, projectFile));

        // Disable proxying during development, making it easier to work with Swagger/Scalar
        // and reducing Aspire errors with ports in use
        if (s_builder.ExecutionContext.IsRunMode)
        {
            p.WithEndpoint("http", e => { e.IsProxied = false; });
            p.WithEndpoint("https", e => { e.IsProxied = false; });
        }

        return p;
    }

    /// <summary>
    /// Add Node.js service.
    /// Important: Node.js projects must have a package.json with a "start"
    ///            script and a Dockerfile for deployments.
    /// </summary>
    /// <param name="name">Service name</param>
    /// <param name="directory">Directory containing the package.json file</param>
    /// <param name="scriptName">Name of the script (defined in package.json) to execute</param>
    private static IResourceBuilder<NodeAppResource> AddNodeJsService(
        string name,
        string directory,
        string scriptName = "start")
    {
        var resource = s_builder
            .AddPnpmApp(name: name, workingDirectory: Path.Join(s_toolsPath, directory), scriptName: scriptName)
            .WithPnpmPackageInstallation() // use "pnpm"
            .PublishAsDockerFile()
            .WithHttpEndpoint(name: "http", env: "PORT"); // pass random port into PORT env var, used by Node.js project
        //.WithHttpEndpoint(name: "http", env: "PORT", isProxied: false); // pass random port into PORT env var, used by Node.js project

        if (s_builder.ExecutionContext.IsPublishMode)
        {
            resource.WithEnvironment("NODE_ENV", "production");
        }

        return resource;
    }

    /// <summary>
    /// Add Python service.
    /// Important: Python projects must have a pyproject.toml file, a specific structure,
    ///            and a Dockerfile for deployments.
    /// </summary>
    /// <param name="name">Service name</param>
    /// <param name="directory">Directory containing the pyproject.toml file</param>
    private static IResourceBuilder<UvicornAppResource> AddPythonService(
        string name,
        string directory)
    {
        string relativePath = Path.Join(s_toolsPath, directory);
        string absolutePath = Path.GetFullPath(relativePath);

        return s_builder
            .PatchedAddUvicornApp(
                name: name,
                projectDirectory: absolutePath,
                appName: "app.main:app")
            .PublishAsDockerFile()
            .WithHttpEndpoint(name: "http", env: "UVICORN_PORT"); // pass random port into PORT env var
    }

    private static void Echo(string text)
    {
        if (s_builder.ExecutionContext.IsRunMode) { Console.WriteLine(text); }
    }
}
