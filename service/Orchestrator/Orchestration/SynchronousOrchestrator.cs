// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Dynamic;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.FunctionAdapters;
using Orchestrator.Models;

namespace Orchestrator.Orchestration;

internal sealed class SynchronousOrchestrator : IDisposable
{
    public const string ActivitySourceName = "SimpleProcessor";
    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    private readonly SimpleWorkspace _workspace;
    private readonly ILogger<SynchronousOrchestrator> _log;
    private readonly HttpAdapter _httpFunctions;

    // CTOR
    public SynchronousOrchestrator(
        SimpleWorkspace workspace,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory? loggerFactory = null)
    {
        this._workspace = workspace;
        this._log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SynchronousOrchestrator>();
        this._httpFunctions = new HttpAdapter(httpClientFactory, loggerFactory);
    }

    /// <summary>
    /// Run workflow synchronously writing state on disk
    /// </summary>
    /// <param name="input">Initial input</param>
    /// <param name="workflow">Workflow definition</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task<(object? result, string workflowId, IResult? error)> RunWorkflowAsync(
        JsonObject input,
        Workflow workflow,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1031 // JMESPath throws generic exceptions
        using Activity? activity = this._activitySource.StartActivity(ActivityKind.Server);
        activity?.AddEvent(new ActivityEvent("Job start"));

        this._log.LogDebug("Job {JobId}: Starting job, Steps: {StepsCount}", workflow.JobId, workflow.Steps.Count);
        await this._workspace.CreateWorkspaceAsync(workflow, input, cancellationToken).ConfigureAwait(false);

        dynamic errorDetails = new ExpandoObject();
        errorDetails.JobId = workflow.JobId;

        JobContext jobContext = await this._workspace.GetContextAsync(workflow.JobId, cancellationToken).ConfigureAwait(false);
        for (int stepNumber = 0; stepNumber < workflow.Steps.Count; stepNumber++)
        {
            activity?.AddEvent(new ActivityEvent("Starting step",
                tags: new ActivityTagsCollection { ["jobId"] = workflow.JobId, ["stepNumber"] = stepNumber, ["stepCount"] = workflow.Steps.Count }));
            this._log.LogDebug("Job {JobId}: Processing step {StepNumber}/{StepCount}", workflow.JobId, stepNumber, workflow.Steps.Count);
            Step step = workflow.Steps[stepNumber];

            FunctionDetails functionDetails = FunctionDetails.Parse(step.Function);

            // Flow:
            // State => step.InputTransformation => In => call func (In) => Out => step.OutputTransformation => State

            var stepContext = new StepContext();
            stepContext.In = jobContext.State;
            jobContext[step.Id] = stepContext;

            errorDetails.StepNumber = stepNumber;
            errorDetails.StepId = step.Id;
            errorDetails.Function = $"{functionDetails.Tool}{functionDetails.Function}";

            // ==========================
            // ==== 1: Prepare input ====
            // ==========================

            if (!string.IsNullOrWhiteSpace(step.InputTransformation))
            {
                this._log.LogDebug("Job {JobId}: Transforming input with JMESPath expression '{Expression}'", workflow.JobId, step.InputTransformation);
                try
                {
                    jobContext.State = await this._workspace.TransformContextAsync(jobContext, step.InputTransformation, cancellationToken).ConfigureAwait(false);
                    this._log.LogDebug("Job {JobId}: Input transformation complete, {State} updated", workflow.JobId, nameof(jobContext.State));
                }
                catch (Exception e)
                {
                    this._log.LogError(e, "Job {JobId}: JMESPath transformation failed", workflow.JobId);
                    activity?.SetStatus(ActivityStatusCode.Error, "Invalid input JMESPath expression");
                    errorDetails.Message = "Invalid input JMESPath expression";
                    errorDetails.Description = e.Message;
                    errorDetails.Expression = step.InputTransformation;
                    return (null, workflow.JobId, Results.BadRequest(errorDetails));
                }
            }
            else
            {
                this._log.LogDebug("Job {JobId}: No input JMESPath transformation to execute", workflow.JobId);
            }

            // Persist context after the transformation, before running the function
            await this._workspace.UpdateContextFileAsync(workflow.JobId, jobContext, cancellationToken).ConfigureAwait(false);

            // ============================
            // ==== 2: Invoke function ====
            // ============================

            switch (functionDetails.Type)
            {
                case FunctionDetails.FunctionTypes.None:
                    break;

                case FunctionDetails.FunctionTypes.Http:
                    // TODO: timeout options and handling
                    (bool success, IResult? error) result = await this._httpFunctions.ExecuteAsync(workflow, step, functionDetails, jobContext, errorDetails, activity, cancellationToken).ConfigureAwait(false);
                    if (!result.success)
                    {
                        this._log.LogError("Job {JobId}: Function '{Function}' failed", workflow.JobId, step.Function);

                        // Add error in a readable format (ie not JSON encoded)
                        if (((IDictionary<string, object>)errorDetails).ContainsKey("Response"))
                        {
                            string[] rows = errorDetails.Response.ToString().Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                            if (rows.Length > 0)
                            {
                                dynamic errorLines = new ExpandoObject();
                                for (int index = 0; index < rows.Length; index++)
                                {
                                    ((IDictionary<string, object>)errorLines)[$"l{index}"] = rows[index];
                                }

                                errorDetails.ResponseLines = errorLines;
                            }
                        }

                        return (null, workflow.JobId, result.error);
                    }

                    break;

                case FunctionDetails.FunctionTypes.Internal:
                    switch (functionDetails.Function)
                    {
                        case "stop":
                            return (jobContext.State, workflow.JobId, null);

                        default:
                            this._log.LogError("Job {JobId}: Unknown internal function {FunctionName}", workflow.JobId, functionDetails.Function);
                            activity?.SetStatus(ActivityStatusCode.Error, $"Unknown internal function {functionDetails.Function}");
                            errorDetails.Message = $"Unknown internal function {functionDetails.Function}";
                            return (null, workflow.JobId, Results.NotFound(errorDetails));
                    }

                default:
                    this._log.LogError("Job {JobId}: Unknown function type {FunctionType}, name {FunctionName}",
                        workflow.JobId, functionDetails.Type.ToString("G"), step.Function);
                    activity?.SetStatus(ActivityStatusCode.Error, "Unknown function type");
                    errorDetails.Message = $"Unknown function type {functionDetails.Type:G}";
                    return (null, workflow.JobId, Results.InternalServerError(errorDetails));
            }

            // Persist context after the function, before xout transformation
            if (functionDetails.Type != FunctionDetails.FunctionTypes.None)
            {
                await this._workspace.UpdateContextFileAsync(workflow.JobId, jobContext, cancellationToken).ConfigureAwait(false);
            }

            // ==================================
            // ==== 3: Output transformation ====
            // ==================================

            // Run JMES expression on full context to calculate the final state
            if (!string.IsNullOrWhiteSpace(step.OutputTransformation))
            {
                this._log.LogDebug("Job {JobId}: Transforming output with JMESPath expression '{Expression}'", workflow.JobId, step.OutputTransformation);
                try
                {
                    jobContext.State = await this._workspace.TransformContextAsync(jobContext, step.OutputTransformation, cancellationToken).ConfigureAwait(false);
                    this._log.LogDebug("Job {JobId}: Output transformation complete, {State} updated", workflow.JobId, nameof(jobContext.State));
                }
                catch (Exception e)
                {
                    this._log.LogError(e, "Job {JobId}: JMESPath transformation failed", workflow.JobId);
                    activity?.SetStatus(ActivityStatusCode.Error, "Invalid output JMESPath expression");
                    errorDetails.Message = "Invalid output JMESPath expression";
                    errorDetails.Description = e.Message;
                    errorDetails.Expression = step.OutputTransformation;
                    return (null, workflow.JobId, Results.BadRequest(errorDetails));
                }
            }
            else
            {
                this._log.LogDebug("Job {JobId}: No output JMESPath transformation to execute", workflow.JobId);
            }

            // Persist context
            stepContext.Out = jobContext.State;
            await this._workspace.UpdateContextFileAsync(workflow.JobId, jobContext, cancellationToken).ConfigureAwait(false);
        }

        activity?.AddEvent(new ActivityEvent("Job end"));
        return (jobContext.State, workflow.JobId, null);
#pragma warning restore CA1031
    }

    public void Dispose()
    {
        this._activitySource.Dispose();
    }
}
