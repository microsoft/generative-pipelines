// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Nodes;
using DevLab.JmesPath;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.Config;
using Orchestrator.Models;
using Orchestrator.Storage;

namespace Orchestrator.Orchestration;

internal sealed class SimpleWorkspace
{
    // Store initial input
    private const string InputFile = "input.json";

    // Store the list of steps
    private const string WorkflowFile = "workflow.json";

    // Store the execution context tracking data and progress
    private const string ContextFile = "context.json";

    private readonly string _dir;
    private readonly ILogger<SimpleWorkspace> _log;
    private readonly IFileSystem _fileSystem;
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { WriteIndented = true };
    private bool _initialized = false;

    public SimpleWorkspace(
        WorkspaceConfig config,
        IFileSystem fileSystem,
        ILoggerFactory? loggerFactory = null)
    {
        config.Validate();
        this._log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SimpleWorkspace>();
        this._fileSystem = fileSystem;
        this._dir = config.WorkspaceDir;
        this._log.LogDebug("Jobs workspace dir: {WorkspaceDir} (Type: {FileSystemType})", this._dir, this._fileSystem.GetType().FullName);
    }

    public async Task CreateWorkspaceAsync(
        Workflow workflow,
        JsonObject input,
        CancellationToken ct)
    {
        await this.EnsureDirectoryExistsAsync(ct).ConfigureAwait(false);

        this._log.LogDebug("Creating workspace for job {JobId}", workflow.JobId);

        // Create a directory for the workflow
        string jobDir = this._fileSystem.CombinePath(this._dir, workflow.JobId);
        await this._fileSystem.CreateDirectoryAsync(jobDir, ct).ConfigureAwait(false);

        var context = new JobContext { Start = input, State = input };

        await this.CreateWorkflowFileAsync(workflow, ct).ConfigureAwait(false);
        await this.CreateInputFileAsync(workflow.JobId, input, ct).ConfigureAwait(false);
        await this.CreateContextFileAsync(workflow.JobId, context, ct).ConfigureAwait(false);
    }

    public async Task<JobContext> GetContextAsync(string jobId, CancellationToken ct)
    {
        await this.EnsureDirectoryExistsAsync(ct).ConfigureAwait(false);

        string workspaceDir = this.GetWorkspacePath(jobId);

        this._log.LogDebug("Fetching context from workspace");
        string contextFile = this._fileSystem.CombinePath(workspaceDir, ContextFile);
        string contextAsString = await this._fileSystem.ReadAllTextAsync(contextFile, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<JobContext>(contextAsString)
               ?? throw new ApplicationException("Failed to deserialize context");
    }

    public async Task CreateInputFileAsync(string jobId, JsonObject input, CancellationToken ct)
    {
        await this.EnsureDirectoryExistsAsync(ct).ConfigureAwait(false);

        string workspaceDir = this.GetWorkspacePath(jobId);

        string inputAsString = input.ToJsonString(s_jsonSerializerOptions);
        string inputFile = this._fileSystem.CombinePath(workspaceDir, InputFile);
        await this._fileSystem.WriteAllTextAsync(inputFile, inputAsString, true, ct).ConfigureAwait(false);
    }

    public async Task CreateWorkflowFileAsync(Workflow workflow, CancellationToken ct)
    {
        await this.EnsureDirectoryExistsAsync(ct).ConfigureAwait(false);

        string workspaceDir = this.GetWorkspacePath(workflow.JobId);

        string workflowAsString = JsonSerializer.Serialize(workflow, s_jsonSerializerOptions);
        string workflowFile = this._fileSystem.CombinePath(workspaceDir, WorkflowFile);
        await this._fileSystem.WriteAllTextAsync(workflowFile, workflowAsString, true, ct).ConfigureAwait(false);
    }

    public async Task CreateContextFileAsync(string jobId, JobContext jobContext, CancellationToken ct)
    {
        await this.EnsureDirectoryExistsAsync(ct).ConfigureAwait(false);

        string workspaceDir = this.GetWorkspacePath(jobId);

        string contextFile = this._fileSystem.CombinePath(workspaceDir, ContextFile);
        string contextAsString = JsonSerializer.Serialize(jobContext, s_jsonSerializerOptions);
        await this._fileSystem.WriteAllTextAsync(contextFile, contextAsString, true, ct).ConfigureAwait(false);
    }

    public async Task UpdateContextFileAsync(string jobId, JobContext jobContext, CancellationToken ct)
    {
        await this.EnsureDirectoryExistsAsync(ct).ConfigureAwait(false);

        string workspaceDir = this.GetWorkspacePath(jobId);

        string contextFile = this._fileSystem.CombinePath(workspaceDir, ContextFile);
        string contextAsString = JsonSerializer.Serialize(jobContext, s_jsonSerializerOptions);
        await this._fileSystem.WriteAllTextAsync(contextFile, contextAsString, false, ct).ConfigureAwait(false);
    }

    public async Task<object?> TransformContextAsync(JobContext jobContext, string jmesExpression, CancellationToken ct)
    {
        await this.EnsureDirectoryExistsAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jmesExpression)) { return jobContext.State; }

        this._log.LogDebug("Updating context with JMES expression: {Expression}", jmesExpression);

        string contextAsString = JsonSerializer.Serialize(jobContext);
        this._log.LogDebug("Context: {Context}", contextAsString);

        var jmes = new JmesPath();
        string? result = await jmes.TransformAsync(contextAsString, jmesExpression).ConfigureAwait(false);
        this._log.LogDebug("JMES transformation result: {Result}", result);
        return JsonSerializer.Deserialize<object>(result);
    }

    private async Task EnsureDirectoryExistsAsync(CancellationToken ct)
    {
        if (this._initialized) { return; }

        this._log.LogDebug("Ensuring workspace directory {Directory} exists", this._dir);
        await this._fileSystem.CreateDirectoryIfNotExistsAsync(this._dir, ct).ConfigureAwait(false);
        this._initialized = true;
    }

    private string GetWorkspacePath(string jobId)
    {
        return this._fileSystem.CombinePath(this._dir, jobId);
    }
}
