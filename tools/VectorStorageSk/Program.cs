// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable CA1031 // TODO

using System.Reflection;
using System.Text;
using CommonDotNet.Diagnostics;
using CommonDotNet.Http;
using CommonDotNet.ServiceDiscovery;
using Microsoft.AspNetCore.Diagnostics;
using VectorStorageSk.Functions;
using VectorStorageSk.Models;
using VectorStorageSk.OpenApi;
using VectorStorageSk.Storage;

namespace VectorStorageSk;

internal static class Program
{
    public static void Main(string[] args)
    {
        // App setup
        var builder = WebApplication.CreateBuilder(args);
        builder.AddLogging(builder.GetAppName());
        builder.Services.AddOpenApi();
        builder.Services.ConfigureSerializationOptions();
        builder.AddRedisToolsRegistry();
        builder.AddInMemoryVectorStore();
        builder.AddQdrantVectorStore(connectionName: "qdrant-storage");
        builder.AddPostgresVectorStore(connectionName: "postgres-storage");
        builder.AddAzureAiSearchVectorStore(connectionName: "aisearch-storage");
        builder.Services.AddScoped<DefinitionsFunction>();
        builder.Services.AddScoped<ListCollectionsFunction>();
        builder.Services.AddScoped<CreateCollectionFunction>();
        builder.Services.AddScoped<DeleteCollectionFunction>();
        builder.Services.AddScoped<UpsertRecordFunction>();

        /*
         * !!!! IMPORTANT !!!!
         *
         * Due to the design of VectorStoreGenericDataModel and the available query translators,
         * filter Expressions defined on "VectorStoreGenericDataModel" are not supported (exceptions are thrown).
         *
         * For now, for every schema required, define a POCO class under DataTypes folder.
         * See the existing MemoryRecord class for reference.
         * Depending on storage, the Key might be Guid or string, so MemoryRecord is a generic class.
         *
         * When invoking /search you must pass these parameters:
         *
         * - [MANDATORY] dataType: the class name of the POCO class, e.g. "MemoryRecord"
         * - [OPTIONAL] primaryKeyType: the type of the primary key, e.g. "string", "Guid", "number"
         *
         * Example:
         *
         *      POST /search
         *      {
         *          storageType:    "qdrant",
         *          collection:     "memories",
         *          dataType:       "MemoryRecord",
         *          primaryKeyType: "default"
         *      }
         *
         * Once Expressions on VectorStoreGenericDataModel are supported, custom POCO classes and `dataType`
         * won't be required anymore.
         *
         * Note about the search function below: we use reflection to dynamically handle
         * multiple model types with a single endpoint. The SearchFunction is not registered with DI,
         * it's handled dynamically with reflection and uses ActivatorUtilities.CreateInstance to work
         * out the correct types.
         */

        // Abb build
        var app = builder.Build();
        app.AddOpenApiDevTools();

        // Orchestrator's tools registry
        var registry = app.Services.GetService<ToolRegistry>();

        // Endpoints / Functions
        const string DefinitionsFunctionName = "definitions";
        registry?.RegisterPostFunction($"/{DefinitionsFunctionName}", "List storage types and field definitions");
        app.MapPost($"/{DefinitionsFunctionName}", IResult (DefinitionsFunction function) => function.Invoke())
            .Produces<List<string>>(StatusCodes.Status200OK)
            .WithName(DefinitionsFunctionName)
            .WithDisplayName("List storage types and field definitions")
            .WithDescription("List available storage types, field types, field properties, etc.")
            .WithSummary("List available storage types, field types, field properties, etc.");

        const string ListCollectionsFunctionName = "list-collections";
        registry?.RegisterPostFunction($"/{ListCollectionsFunctionName}", "List existing collections");
        app.MapPost($"/{ListCollectionsFunctionName}", async Task<IResult> (
                    ListCollectionsFunction function,
                    ListCollectionsRequest req,
                    CancellationToken cancellationToken)
                => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
            .Produces<List<string>>(StatusCodes.Status200OK)
            .WithName(ListCollectionsFunctionName)
            .WithDisplayName("List existing collections")
            .WithDescription("List existing collections in the vector storages")
            .WithSummary("List existing collections in the vector storages");

        const string CreateCollectionFunctionName = "create-collection";
        registry?.RegisterPostFunction($"/{CreateCollectionFunctionName}", "Create collection");
        app.MapPost($"/{CreateCollectionFunctionName}", async Task<IResult> (
                CreateCollectionFunction function,
                CreateCollectionRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, false, cancellationToken).ConfigureAwait(false))
            .Produces<CreateCollectionResponse>(StatusCodes.Status200OK)
            .WithName(CreateCollectionFunctionName)
            .WithDisplayName("Create vector collection")
            .WithDescription("Create a new vector collection in the vector store, if not already present")
            .WithSummary("Create a new vector collection in the vector store, if not already present");

        const string CreateCollectionWithCheckFunctionName = "create-collection-with-check";
        registry?.RegisterPostFunction($"/{CreateCollectionWithCheckFunctionName}", "Create collection");
        app.MapPost($"/{CreateCollectionWithCheckFunctionName}", async Task<IResult> (
                CreateCollectionFunction function,
                CreateCollectionRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, true, cancellationToken).ConfigureAwait(false))
            .Produces<CreateCollectionResponse>(StatusCodes.Status200OK)
            .WithName(CreateCollectionWithCheckFunctionName)
            .WithDisplayName("Create vector collection")
            .WithDescription("Create a new vector collection in the vector store, failing with an error if already present")
            .WithSummary("Create a new vector collection in the vector store, failing with an error if already present");

        const string DeleteCollectionFunctionName = "delete-collection";
        registry?.RegisterPostFunction($"/{DeleteCollectionFunctionName}", "Delete collection");
        app.MapPost($"/{DeleteCollectionFunctionName}", async Task<IResult> (
                DeleteCollectionFunction function,
                DeleteCollectionRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
            .Produces<DeleteCollectionResponse>(StatusCodes.Status200OK)
            .WithName(DeleteCollectionFunctionName)
            .WithDisplayName("Delete vector collection")
            .WithDescription("Delete an existing collection in the vector store, if present")
            .WithSummary("Delete an existing collection in the vector store, if present");

        const string UpsertFunctionName = "upsert";
        registry?.RegisterPostFunction($"/{UpsertFunctionName}", "Upsert record");
        app.MapPost($"/{UpsertFunctionName}", async Task<IResult> (
                UpsertRecordFunction function,
                UpsertRecordRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
            .Produces<UpsertRecordResponse>(StatusCodes.Status200OK)
            .WithName(UpsertFunctionName)
            .WithDisplayName("Upsert record")
            .WithDescription("Update an existing record or create if not exists")
            .WithSummary("Update an existing record or create if not exists");

        const string SearchFunctionName = "search";
        registry?.RegisterPostFunction($"/{SearchFunctionName}", "Vector search");
        app.MapPost($"/{SearchFunctionName}", async Task<IResult> (
                    SearchRequest req,
                    CancellationToken cancellationToken) =>
                {
                    // Note: SK doesn't support search on generic data classes yet, so we use reflection to dynamically handle
                    // multiple model types with a single endpoint. The SearchFunction is not registered with DI, it's handled
                    // dynamically with reflection and uses ActivatorUtilities.CreateInstance to work out the correct types.

                    // Which class to use? MemoryRecord<Guid>, MemoryRecord<string>, MyClass, YourClass<ulong>, etc.
                    Type keyType = GetKeyType(req.PrimaryKeyType, req.StorageType);
                    Type? dataType = GetDataType(req.DataType, keyType);
                    if (dataType == null) { return Results.BadRequest($"Unknown data type: {req.DataType}"); }

                    // Dynamically build SearchFunction<TKey, TRecord> and InvokeAsync()
                    (object methodContainer, MethodInfo? method) caller = GetSearchMethod(keyType, dataType, app.Services);
                    if (caller.method == null) { return Results.BadRequest($"Search function not available for data type {req.DataType}"); }

                    // Dynamically call SearchFunction<T>.InvokeAsync()
                    var result = caller.method.Invoke(caller.methodContainer, [req, cancellationToken]);
                    if (result is not Task<IResult> task)
                    {
                        return Results.InternalServerError($"Failed to invoke search function, {nameof(SearchFunction<object, object>.InvokeAsync)} method returns null or an unexpected type");
                    }

                    return await task.ConfigureAwait(false);

                    #region internals

                    // Dynamically create the search function and get the InvokeAsync method
                    static (object methodContainer, MethodInfo? method) GetSearchMethod(Type keyType, Type dataType, IServiceProvider serviceProvider)
                    {
                        // Instantiate the search function, generic on data model <T>, using the model class name from the request
                        var functionGenericType = typeof(SearchFunction<,>);
                        var functionCloseType = functionGenericType.MakeGenericType(keyType, dataType);
                        var function = ActivatorUtilities.CreateInstance(serviceProvider, functionCloseType);

                        // Return InvokeAsync method used to search records
                        return (function, functionCloseType.GetMethod("InvokeAsync"));
                    }

                    // Get the type (string, Guid, ulong, etc.) of the data model key (aka Record ID)
                    static Type GetKeyType(PrimaryKeyTypes pkType, StorageTypes storageType) => pkType switch
                    {
                        PrimaryKeyTypes.String => typeof(string),
                        PrimaryKeyTypes.Guid => typeof(Guid),
                        PrimaryKeyTypes.Number => typeof(ulong),
                        _ => storageType == StorageTypes.Qdrant ? typeof(Guid) : typeof(string)
                    };

                    // Get the data type for the given data type name, primary key type and storage type
                    static Type? GetDataType(string dataType, Type primaryKeyType)
                    {
                        Type? result = Type.GetType(dataType)
                                       ?? Type.GetType($"{dataType}`1")
                                       ?? Type.GetType($"VectorStorageSk.SemanticKernel.{dataType}")
                                       ?? Type.GetType($"VectorStorageSk.SemanticKernel.{dataType}`1");

                        // Error: data model type not found
                        if (result == null) { return null; }

                        return !result.IsGenericType ? result : result.MakeGenericType(primaryKeyType);
                    }

                    #endregion
                }
            )
            .Produces<SearchResponse>(StatusCodes.Status200OK)
            .WithName(SearchFunctionName)
            .WithDisplayName("Vector search for MemoryRecord<Guid>")
            .WithDescription("Search records by vector similarity")
            .WithSummary("Search records by vector similarity");

        // Error handling
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var exceptionDetails = new StringBuilder();
                while (exception != null)
                {
                    exceptionDetails.AppendLine($"{exception.GetType().Name}: {exception?.Message}");
                    exception = exception?.InnerException;
                }

                if (exception is BadHttpRequestException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Message = "Invalid request payload",
                        Description = exceptionDetails.ToString()
                    }).ConfigureAwait(false);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Message = "An unexpected error occurred",
                        Description = exceptionDetails.ToString()
                    }).ConfigureAwait(false);
                }
            });
        });

        app.Run();
    }
}
