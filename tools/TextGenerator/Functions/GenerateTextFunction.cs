// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommonDotNet.Diagnostics;
using CommonDotNet.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Chat;
using TextGenerator.Client;
using TextGenerator.Config;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace TextGenerator.Functions;

internal sealed class GenerateTextFunction
{
    private readonly AppConfig _appConfig;
    private readonly ILogger<GenerateTextFunction> _log;
    private readonly ILoggerFactory _loggerFactory;

    public GenerateTextFunction(AppConfig appConfig, ILoggerFactory? loggerFactory = null)
    {
        this._appConfig = appConfig;
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        this._log = this._loggerFactory.CreateLogger<GenerateTextFunction>();
    }

    public async Task<IResult> InvokeAsync(GenerateTextRequest req, CancellationToken cancellationToken = default)
    {
        if (req == null) { return Results.BadRequest("The request is empty"); }

        if (!req.FixState().IsValid(out var errMsg)) { return Results.BadRequest(errMsg); }

        var models = this._appConfig.GetModelsInfo();
        if (!models.TryGetValue(req.ModelId, out ModelInfo? model))
        {
            return Results.BadRequest($"Model {req.ModelId} is not available. Check /models for the list of available models.");
        }

        model.EnsureValid();

        // Prepare client
        IChatClient? chatClient = null;
        try
        {
            if (!this.TryGetClient(model, req, out chatClient, out var error)) { return error; }

            // Prepare request
            IEnumerable<ChatMessage> chatHistory = this.PrepareChatHistory(req);
            var chatOptions = new ChatOptions
            {
                ChatThreadId = null,
                MaxOutputTokens = req.MaxTokens,
                Temperature = req.Temperature,
                TopP = req.NucleusSampling,
                TopK = req.TruncatedSampling,
                PresencePenalty = req.PresencePenalty,
                FrequencyPenalty = req.FrequencyPenalty,
                Seed = req.Seed,
            };

            // Execute
            var result = new StringBuilder();
            var response = new GenerateTextResponse();

            IAsyncEnumerable<ChatResponseUpdate> resultStream = chatClient.GetStreamingResponseAsync(chatHistory, chatOptions, cancellationToken: cancellationToken);
            var finishReason = new StringBuilder();
            await foreach (ChatResponseUpdate x in resultStream.ConfigureAwait(false))
            {
                if (x.AdditionalProperties?.TryGetValue("FinishReason", out var finishReasonValue) ?? false)
                {
                    finishReason.Append(finishReasonValue);
                }

                if (x.AdditionalProperties != null && x.AdditionalProperties.TryGetValue("CompletionId", out var completionId) && !string.IsNullOrWhiteSpace(completionId?.ToString()))
                {
                    response.Report ??= new Report();
                    response.Report.CompletionId = completionId.ToString();
                }

                if (x.AdditionalProperties?.TryGetValue("Usage", out var usageValue) ?? false)
                {
                    if (usageValue is ChatTokenUsage tokens)
                    {
                        response.Report ??= new Report();
                        response.Report.InputTokenCount += tokens.InputTokenCount;
                        response.Report.OutputTokenCount += tokens.OutputTokenCount;
                        response.Report.TotalTokenCount += tokens.TotalTokenCount;

                        if (tokens.InputTokenDetails != null)
                        {
                            response.Report.InputAudioTokenCount = tokens.InputTokenDetails.AudioTokenCount;
                            response.Report.InputCachedTokenCount = tokens.InputTokenDetails.CachedTokenCount;
                        }

                        if (tokens.OutputTokenDetails != null)
                        {
                            response.Report.OutputAudioTokenCount = tokens.OutputTokenDetails.AudioTokenCount;
                            response.Report.OutputAcceptedPredictionTokenCount = tokens.OutputTokenDetails.AcceptedPredictionTokenCount;
                            response.Report.OutputReasoningTokenCount = tokens.OutputTokenDetails.ReasoningTokenCount;
                            response.Report.OutputRejectedPredictionTokenCount = tokens.OutputTokenDetails.RejectedPredictionTokenCount;
                        }
                    }
                }

                result.Append(x.Text);
            }

            response.Text = result.ToString();

            response.Report ??= new Report();
            response.Report.FinishReason = finishReason.ToString();

            this._log.LogTrace("Answer: {Answer}", response.Text);

            return Results.Ok(response);
        }
        finally
        {
            chatClient?.Dispose();
        }
    }

    private bool TryGetClient(
        ModelInfo model,
        GenerateTextRequest req,
        [NotNullWhen(true)] out IChatClient? client,
        [NotNullWhen(false)] out IResult? error)
    {
        error = null;
        client = null;
        try
        {
            switch (model.Provider)
            {
                case ModelInfo.ModelProviders.AzureAI:
                {
                    var modelSettings = this._appConfig.AzureAI.GetModelById(req.ModelId);
                    client = ClientFactory.GetChatClient(modelSettings, this._loggerFactory);
                    return true;
                }
                case ModelInfo.ModelProviders.OpenAI:
                {
                    var modelSettings = this._appConfig.OpenAI.GetModelById(req.ModelId);
                    client = ClientFactory.GetChatClient(modelSettings, this._loggerFactory);
                    return true;
                }

                case ModelInfo.ModelProviders.Ollama:
                    client = ClientFactory.GetOllamaChatService(model.Endpoint, req.ModelId, this._loggerFactory);
                    return true;

                default:
                    error = Results.BadRequest($"Unknown model provider: {model.Provider}");
                    return false;
            }
        }
        catch (BadRequestException ex)
        {
            error = Results.BadRequest($"Unable to create AI client: {ex.Message}");
            return false;
        }
    }

    private List<ChatMessage> PrepareChatHistory(GenerateTextRequest req)
    {
        var chatHistory = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(req.SystemPrompt))
        {
            this._log.LogTrace("System prompt: {System}", req.SystemPrompt);
            chatHistory.Add(new ChatMessage(ChatRole.System, req.SystemPrompt));
        }

        if (!string.IsNullOrWhiteSpace(req.Prompt))
        {
            this._log.LogTrace("Prompt: {Prompt}", req.Prompt);
            chatHistory.Add(new ChatMessage(ChatRole.User, req.Prompt));
        }

        return chatHistory;
    }
}
