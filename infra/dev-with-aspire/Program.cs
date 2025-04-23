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
    private const string RedisConnStringName = "redisstorage";
    private const string QdrantConnStringName = "qdrantstorage";
    private const string PostgresConnStringName = "postgresstorage";
    private const string AzureAiSearchConnStringName = "aisearchstorage";
    private const string AzureBlobsConnStringName = "blobstorage";
    private const string AzureQueuesConnStringName = "queuestorage";

    // Directory containing the tools to be available via the orchestrator.
    private static readonly string s_toolsPath = Path.Join("..", "..", "tools");

    // Use path rather than a reference, to decouple nuget dependencies management
    private static readonly string s_orchestratorProjectFile = Path.Join("..", "service", "Orchestrator", "Orchestrator.csproj");

    // HOME dir, used for docker volumes (used also by docker compose files)
    private static readonly string s_userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    // Where local docker image will persist their data across multiple executions, e.g. Redis cache, Postgres DBs, Qdrant collections, etc.
    // If you change this path, make sure all resources can write to it. Don't use Program.cs to create the dir to avoid incorrect permissions.
    // Note: this path is used also by the docker-compose files, so it should be consistent if you expect data to be shared.
    // private static readonly string s_localDockerData = Path.GetFullPath(Path.Join("..", "..", "tools", "_docker_data"));
    private static readonly string s_localDockerData = Path.Join(s_userProfileDir, "docker-volumes");

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

        var azureStorage = AddAzureStorage(); // Blobs and Queues
        var redis = AddRedisResources(); // Cache and KV
        var postgres = AddPostgresResources(); // Vector storage
        var qdrant = AddQdrantResources(); // Vector storage
        var azSearch = AddAzureAiSearch(); // Vector storage with semantic ranking
        var orchestrator = AddOrchestrator(azureStorage.blobs, azureStorage.queues, redis); // Orchestrator
        var ollama = AddOllama();

        List<IResourceBuilder<IResourceWithConnectionString>?> references = [redis, azSearch, postgres, qdrant, azureStorage.blobs, azureStorage.queues, ollama];

        // Tools
        var toolNames = new List<string>();
        Echo("=================================");
        AddCSharpTools(orchestrator, references).AddToList(toolNames);
        AddFSharpTools(orchestrator, references).AddToList(toolNames);
        AddNodeJsTools(orchestrator, references).AddToList(toolNames);
        AddPythonTools(orchestrator, references).AddToList(toolNames);
        Echo("=================================");

        s_builder.Configure(toolNames);

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
        IResourceBuilder<ProjectResource> orchestrator = AddCSharpTool("orchestrator", s_orchestratorProjectFile)
            .WithExternalHttpEndpoints(); // The orchestrator is the only public endpoint

        orchestrator.WithReference(blobs).WaitFor(blobs);
        orchestrator.WithReference(queues).WaitFor(queues);

        if (redis != null)
        {
            orchestrator.WithReference(redis).WaitFor(redis);
        }

        // Force authentication when running on Azure, overriding Orchestrator's appsettings.json
        // Keys for Azure should be set with dotnet user-secrets (or appsettings.json).
        // IMPORTANT: see https://github.com/dotnet/aspire/issues/8824 - you might have to set the keys manually editing azd vault files.
        if (s_builder.ExecutionContext.IsPublishMode)
        {
            // .NET Aspire BUG: see https://github.com/dotnet/aspire/issues/8824
            // if (string.IsNullOrWhiteSpace(s_builder.Configuration["Parameters:AccessKey1"]))
            // {
            //     throw new ArgumentNullException("Parameters:accesskey1", "Access Key 1 is empty");
            // }
            //
            // if (string.IsNullOrWhiteSpace(s_builder.Configuration["Parameters:AccessKey2"]))
            // {
            //     throw new ArgumentNullException("Parameters:accesskey2", "Access Key 2 is empty");
            // }
            // var key1 = s_builder.AddParameter("accesskey1", secret: true, value: s_builder.Configuration["Parameters:accesskey1"]);
            // var key2 = s_builder.AddParameter("accesskey2", secret: true, value: s_builder.Configuration["Parameters:accesskey2"]);

            // Note: values are retrieved from the the local key vault, under ~/.azd/vaults - see https://github.com/dotnet/aspire/issues/8824
            var key1 = s_builder.AddParameter("accesskey1", secret: true);
            var key2 = s_builder.AddParameter("accesskey2", secret: true);

            orchestrator
                .WithEnvironment("App__Authorization__AccessKey1", key1)
                .WithEnvironment("App__Authorization__AccessKey2", key2)
                .WithEnvironment("App__Authorization__Type", "AccessKey");
        }

        orchestrator.WithUrl("https://github.com/microsoft/generative-pipelines?tab=readme-ov-file#documentation", "Docs");
        orchestrator.WithUrl("https://github.com/microsoft/generative-pipelines", "GitHub");

        return orchestrator;
    }

    private static IResourceBuilder<OllamaResource> AddOllama()
    {
        IResourceBuilder<OllamaResource> ollama = s_builder.AddOllama("ollama")
            .WithImage(image: s_config.OllamaContainerImage, tag: s_config.OllamaContainerTag)
            .WithDataVolume();

        ollama.AddModel("phi4mini", modelName: "phi4-mini");

        return ollama;
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

        // Options to browse Azure Storage:
        // - Storage Explorer https://azure.microsoft.com/products/storage/storage-explorer
        // - VS Code extensions
        // - Jetbrains Rider Azure Toolkit

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
                    .WithPersistence(interval: TimeSpan.FromSeconds(45), keysChangedThreshold: 1)
                    .WithDataBindMount(Path.Join(s_localDockerData, "redis"))
                    .WithRedisCommander()
                    .WithRedisInsight();
            }
            else
            {
                redis = s_builder.AddRedis(RedisConnStringName)
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
                .WithDataBindMount(Path.Join(s_localDockerData, ResourceName));
        }

        // On localhost
        if (s_builder.ExecutionContext.IsRunMode)
        {
            // Note: default value shared with docker-compose files
            var apiKey = s_builder.AddParameter($"{ResourceName}-Key", "changeme");

            // var customSecret = s_builder.AddParameter($"{ResourceName}-Key", "value here");
            // qdrant = s_builder.AddQdrant(QdrantConnStringName, apiKey: customSecret)

            qdrant = s_builder.AddQdrant(QdrantConnStringName, apiKey: apiKey)
                .WithDataBindMount(Path.Join(s_localDockerData, ResourceName));
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
            // Note: defaults shared with docker-compose files
            var username = s_builder.AddParameter($"{ResourceName}-user", "postgres");
            var password = s_builder.AddParameter($"{ResourceName}-password", "changeme");

            postgres = s_builder.AddPostgres(PostgresConnStringName, userName: username, password: password)
                .WithImage(image: s_config.PostgresContainerImage, tag: s_config.PostgresContainerImageTag)
                .WithPgAdmin()
                .WithDataBindMount(Path.Join(s_localDockerData, ResourceName));
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
    private static List<string> AddCSharpTools(IResourceBuilder<ProjectResource> orchestrator,
        ICollection<IResourceBuilder<IResourceWithConnectionString>?> references)
    {
        try
        {
            var csProjects = ToolDiscovery.FindCSharpTools(s_toolsPath).ToList();
            foreach (var p in csProjects)
            {
                Echo($"- Adding {p.name} (C# {p.projectFilePath})");

                IResourceBuilder<ProjectResource> resource = AddCSharpTool(p.name, p.projectFilePath)
                    .WithEnvironment(ToolNameEnvVar, p.name);

                // Inject connection strings
                foreach (var r in references.Where(x => x != null)) { resource.WithReference(r!); }

                orchestrator.WithReference(resource);
            }

            return csProjects.Select(x => x.name).ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering C# projects: {ex.Message}");
            Environment.Exit(1);
            return null!;
        }
    }

    /// <summary>
    /// Discover and add all .NET tools.
    /// </summary>
    private static List<string> AddFSharpTools(IResourceBuilder<ProjectResource> orchestrator,
        ICollection<IResourceBuilder<IResourceWithConnectionString>?> references)
    {
        try
        {
            var fsProjects = ToolDiscovery.FindFSharpTools(s_toolsPath).ToList();
            foreach (var p in fsProjects)
            {
                Echo($"- Adding {p.name} (F# {p.projectFilePath})");

                IResourceBuilder<ProjectResource> resource = AddFSharpTool(p.name, p.projectFilePath)
                    .WithEnvironment(ToolNameEnvVar, p.name);

                // Inject connection strings
                foreach (var r in references.Where(x => x != null)) { resource.WithReference(r!); }

                orchestrator.WithReference(resource);
            }

            return fsProjects.Select(x => x.name).ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering F# projects: {ex.Message}");
            Environment.Exit(1);
            return null!;
        }
    }

    /// <summary>
    /// Discover and add all Python tools.
    /// </summary>
    private static List<string> AddPythonTools(IResourceBuilder<ProjectResource> orchestrator,
        ICollection<IResourceBuilder<IResourceWithConnectionString>?> references)
    {
        try
        {
            var pyProjects = ToolDiscovery.FindPythonTools(s_toolsPath).ToList();
            foreach (var p in pyProjects)
            {
                Echo($"- Adding {p.name} ({p.dirName} Python tool)");

                var resource = AddPythonTool(p.name, p.dirName)
                    .WithEnvironment(ToolNameEnvVar, p.name);

                // Inject connection strings
                foreach (var r in references.Where(x => x != null)) { resource.WithReference(r!); }

                orchestrator.WithReference(resource);
            }

            return pyProjects.Select(x => x.name).ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Python projects: {ex.Message}");
            Environment.Exit(1);
            return null!;
        }
    }

    /// <summary>
    /// Discover and add all Node.js tools.
    /// </summary>
    private static List<string> AddNodeJsTools(IResourceBuilder<ProjectResource> orchestrator,
        ICollection<IResourceBuilder<IResourceWithConnectionString>?> references)
    {
        try
        {
            var nodeProjects = ToolDiscovery.FindNodeJsTools(s_toolsPath).ToList();
            foreach (var p in nodeProjects)
            {
                Echo($"- Adding {p.name} ({p.dirName} Node.js tool)");

                var resource = AddNodeJsTool(p.name, p.dirName)
                    .WithEnvironment(ToolNameEnvVar, p.name);

                // Inject connection strings
                foreach (var r in references.Where(x => x != null)) { resource.WithReference(r!); }

                orchestrator.WithReference(resource);
            }

            return nodeProjects.Select(x => x.name).ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Node.js projects: {ex.Message}");
            Environment.Exit(1);
            return null!;
        }
    }

    /// <summary>
    /// Add a .NET service.
    /// Important: .NET projects must have their launchSettings defining the http and https endpoints
    /// </summary>
    /// <param name="toolName">Service name</param>
    /// <param name="cSharpProjectFile">Path to the .csproj file</param>
    private static IResourceBuilder<ProjectResource> AddCSharpTool(
        string toolName,
        string cSharpProjectFile)
    {
        var p = s_builder.AddProject(name: toolName, projectPath: Path.Join(s_toolsPath, cSharpProjectFile));

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
    /// Add a .NET F# tool.
    /// Important: .NET projects must have their launchSettings defining the http and https endpoints
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <param name="fSharpProjectFile">Path to the .csproj file</param>
    private static IResourceBuilder<ProjectResource> AddFSharpTool(
        string toolName,
        string fSharpProjectFile)
    {
        var p = s_builder.AddProject(name: toolName, projectPath: Path.Join(s_toolsPath, fSharpProjectFile));

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
    /// Add Node.js tool.
    /// Important: Node.js projects must have a package.json with a "start"
    ///            script and a Dockerfile for deployments.
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <param name="toolDirectory">Directory containing the package.json file</param>
    /// <param name="startScriptName">Name of the script (defined in package.json) to execute</param>
    private static IResourceBuilder<NodeAppResource> AddNodeJsTool(
        string toolName,
        string toolDirectory,
        string startScriptName = "start")
    {
        var resource = s_builder
            .AddPnpmApp(name: toolName, workingDirectory: Path.Join(s_toolsPath, toolDirectory), scriptName: startScriptName)
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
    /// Add Python tool.
    /// Important: Python projects must have a pyproject.toml file, a specific structure,
    ///            and a Dockerfile for deployments.
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <param name="toolDirectory">Directory containing the pyproject.toml file</param>
    private static IResourceBuilder<UvicornAppResource> AddPythonTool(
        string toolName,
        string toolDirectory)
    {
        string relativePath = Path.Join(s_toolsPath, toolDirectory);
        string absolutePath = Path.GetFullPath(relativePath);

        return s_builder
            .PatchedAddUvicornApp(
                name: toolName,
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
