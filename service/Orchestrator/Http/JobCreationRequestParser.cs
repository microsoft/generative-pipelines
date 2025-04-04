// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using Orchestrator.Models;

namespace Orchestrator.Http;

internal static class JobCreationRequestParser
{
    private static readonly JsonNodeOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonDocumentOptions s_jsonDocOpts = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private const string JsonPrefixHeader = "X-Content-Type-JSON-prefix";
    private const string JsonFieldHeaderPrefix = "X-Content-Type-";
    private const string JsonContentType = "application/json";
    private const string DefaultJsonMultipartFieldPrefix = "$";
    private const string WorkflowField = "_workflow";
    private const string FileNameField = "fileName";
    private const string FileContentField = "content";
    private const string FileArrayMultipartField = "files";
    private const string FileArrayParsedField = "files";

    /// <summary>
    /// Parse JSON input from the request body.
    /// Take _workflow field and parse it into a Workflow.Steps property.
    /// Take the rest of the JSON and assign it to Workflow.Input property.
    /// </summary>
    public static async Task<(Workflow? workflow, JsonObject? input, IResult? error)> ParseJsonInputAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(body))
        {
            return (null, null, Results.BadRequest("Request body cannot be empty, and must be a valid JSON object"));
        }

        // Payload JSON deserialization and validation
        JsonObject? input;
        try
        {
            input = JsonNode.Parse(body, s_jsonOpts, s_jsonDocOpts) as JsonObject;
            if (input == null || !input.ContainsKey(WorkflowField))
            {
                return (null, null, Results.BadRequest($"JSON must contain a '{WorkflowField}' field describing the operations to execute"));
            }
        }
        catch (JsonException)
        {
            return (null, null, Results.BadRequest("Invalid JSON format"));
        }

        // Workflow JSON deserialization
        Workflow workflow = new();
        try
        {
            if (input.TryGetPropertyValue(WorkflowField, out var workflowNode))
            {
                workflow = workflowNode.Deserialize<Workflow>() ?? new Workflow();
            }
        }
        catch (JsonException)
        {
            return (null, null, Results.BadRequest($"Invalid JSON format in '{WorkflowField}' field"));
        }

        AssignIdToJob(workflow);

        input.Remove(WorkflowField);

        if (!AssignIdToSteps(workflow, out string errorMessage))
        {
            return (null, null, Results.BadRequest(errorMessage));
        }

        return (workflow, input, null);
    }

    public static async Task<(Workflow? workflow, JsonObject? input, IResult? error)> ParseMultipartInputAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        IFormCollection form = await context.Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        JsonObject input = new();
        Workflow workflow = new();

        if (form.Files.Count == 1)
        {
            foreach (var file in form.Files)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                input[FileContentField] = Convert.ToBase64String(ms.ToArray());
                input[FileNameField] = file.FileName;
            }
        }
        else if (form.Files.Count > 1)
        {
            var fileArray = new JsonArray();

            foreach (var file in form.Files)
            {
                var fileData = new JsonObject
                {
                    [FileNameField] = file.FileName
                };

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                fileData[FileContentField] = Convert.ToBase64String(ms.ToArray());

                fileArray.Add(fileData);
            }

            input[FileArrayParsedField] = fileArray;
        }

        /*
         * Multipart form data can contain multiple fields, of different types.
         *
         * .NET IFormCollection doesn't provide a way to get the content type of each field,
         * so we use a few conventions:
         * 1. For files, only Base 64 encoded content is supported.
         * 2. For text fields, only Plain Test and JSON encoded content is supported.
         * 3. The "_workflow" field is a special field, always JSON encoded.
         * 4. Unless specified otherwise (see option 1), all other text fields are retrieved as plain text.
         * 5. To define the encoding of all or individual fields:
         *
         *   - Option 1:
         *     - A "X-Content-Type-*: application/json" header indicates that all fields are JSON serialized.
         *
         *   - Option 2:
         *     - A "X-Content-Type-{field name}: application/json" header indicates that a specific field is JSON serialized.
         *
         *   - Option 3:
         *     - A field name can have a prefix, to indicate the value is JSON encoded.
         *     - The default prefix is "$"
         *     - The default prefix can be overridden using the X-Content-Type-JSON-prefix header.
         */
        var defaultJsonFieldPrefix = DefaultJsonMultipartFieldPrefix;
        if (context.Request.Headers.TryGetValue(JsonPrefixHeader, out StringValues values))
        {
            var newPrefix = values.ToString().Trim();
            if (string.IsNullOrEmpty(newPrefix))
            {
                return (null, null, Results.BadRequest($"Invalid value in '{JsonPrefixHeader}' header, the value cannot be empty"));
            }

            defaultJsonFieldPrefix = values.ToString().Trim();
        }

        // Loop through all fields, except for "files" field
        foreach (KeyValuePair<string, StringValues> field in form)
        {
            if (field.Key == FileArrayMultipartField) { continue; }

            // Workflow definition, always JSON encoded
            if (field.Key == WorkflowField)
            {
                if (field.Value.Count == 0) { continue; }

                if (field.Value.Count > 1)
                {
                    return (null, null, Results.BadRequest($"Only one '{WorkflowField}' field is allowed"));
                }

                try
                {
                    workflow = JsonSerializer.Deserialize<Workflow>(field.Value.ToString()) ?? new Workflow();
                }
                catch (JsonException)
                {
                    return (null, null, Results.BadRequest($"Invalid JSON format in '{WorkflowField}' field"));
                }

                AssignIdToJob(workflow);

                continue;
            }

            // Other fields, if JSON encoded
            if (context.Request.Headers.TryGetValue($"{JsonFieldHeaderPrefix}{field.Key}", out var contentType)
                && contentType.ToString().StartsWith(JsonContentType, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    input[field.Key] = JsonNode.Parse(field.Value.ToString(), s_jsonOpts, s_jsonDocOpts);
                }
                catch (JsonException)
                {
                    return (null, null, Results.BadRequest($"Invalid JSON format in '{field.Key}' field"));
                }

                continue;
            }

            // Other fields, if JSON encoded
            if (field.Key.StartsWith(defaultJsonFieldPrefix, StringComparison.Ordinal))
            {
                var fieldName = field.Key.Substring(defaultJsonFieldPrefix.Length);
                try
                {
                    input[fieldName] = JsonNode.Parse(field.Value.ToString(), s_jsonOpts, s_jsonDocOpts);
                }
                catch (JsonException)
                {
                    return (null, null, Results.BadRequest($"Invalid JSON format in '{field.Key}' field"));
                }

                continue;
            }

            // Other fields, if Plain Text
            input[field.Key] = field.Value.ToString();
        }

        if (!AssignIdToSteps(workflow, out string errorMessage))
        {
            return (null, null, Results.BadRequest(errorMessage));
        }

        return (workflow, input, null);
    }

    private static void AssignIdToJob(Workflow workflow)
    {
        if (string.IsNullOrEmpty(workflow.JobId))
        {
            workflow.JobId = Guid.NewGuid().ToString("D");
        }
    }

    private static bool AssignIdToSteps(Workflow workflow, out string errorMessage)
    {
        errorMessage = string.Empty;

        // Collect all step IDs to check for duplicates
        var stepIdsUsed = new HashSet<string>();
        foreach (var step in workflow.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id)) { continue; }

            if (!stepIdsUsed.Add(step.Id))
            {
                errorMessage = $"Duplicate step ID '{step.Id}' found";
                return false;
            }
        }

        // Assign a unique ID to each step
        foreach (var step in workflow.Steps)
        {
            if (!string.IsNullOrEmpty(step.Id)) { continue; }

            step.Id = GetStepId(step, stepIdsUsed);
            stepIdsUsed.Add(step.Id);
        }

        return true;
    }

    /// <summary>
    /// Generate a step ID based on the function name.
    /// </summary>
    private static string GetStepId(Step step, HashSet<string> existingIds)
    {
        if (!string.IsNullOrEmpty(step.Id)) { return step.Id; }

        int i = 1;
        string generatedId = step.Function;
        while (existingIds.Contains(generatedId))
        {
            generatedId = $"{step.Function}{i++}";
        }

        return generatedId;
    }
}
