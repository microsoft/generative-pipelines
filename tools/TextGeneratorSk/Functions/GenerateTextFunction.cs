// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommonDotNet.Diagnostics;
using CommonDotNet.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using TextGeneratorSk.Client;
using TextGeneratorSk.Models;

namespace TextGeneratorSk.Functions;

internal sealed class GenerateTextFunction
{
    private readonly AppConfig _appConfig;
    private readonly ILogger<GenerateTextFunction> _log;
    private readonly ILoggerFactory? _loggerFactory;

    public GenerateTextFunction(AppConfig appConfig, ILoggerFactory? loggerFactory = null)
    {
        this._appConfig = appConfig;
        this._loggerFactory = loggerFactory;
        this._log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<GenerateTextFunction>();
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
        if (!this.TryGetClient(model, req, out var chatService, out var error)) { return error; }

        // Prepare request
        ChatHistory chatHistory = GetChatHistory(req);
        if (!TryGetRequestOptions(model, req, out var executionSettings, out var error2)) { return error2; }

        // Execute
        var result = new StringBuilder();

        IAsyncEnumerable<StreamingChatMessageContent> resultStream = chatService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, null, cancellationToken);
        await foreach (StreamingChatMessageContent x in resultStream.ConfigureAwait(false))
        {
            result.Append(x.Content);
        }

        return Results.Ok(new GenerateTextResponse { Text = result.ToString() });
    }

    private bool TryGetClient(
        ModelInfo model,
        GenerateTextRequest req,
        [NotNullWhen(true)] out IChatCompletionService? client,
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
                    client = ClientFactory.GetChatService(modelSettings, this._loggerFactory);
                    return true;
                }
                case ModelInfo.ModelProviders.OpenAI:
                {
                    var modelSettings = this._appConfig.OpenAI.GetModelById(req.ModelId);
                    client = ClientFactory.GetChatService(modelSettings, this._loggerFactory);
                    return true;
                }

                case ModelInfo.ModelProviders.Ollama:
                    client = ClientFactory.GetOllamaChatService(model.Endpoint, req.ModelId, out var ollamaClient);
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

    private static bool TryGetRequestOptions(
        ModelInfo model,
        GenerateTextRequest req,
        [NotNullWhen(true)] out PromptExecutionSettings? settings,
        [NotNullWhen(false)] out IResult? error)

    {
        settings = null;
        error = null;
        switch (model.Provider)
        {
            case ModelInfo.ModelProviders.AzureAI:
                settings = new AzureOpenAIPromptExecutionSettings
                {
                    MaxTokens = req.MaxTokens, // note: nullable
                    Temperature = req.Temperature,
                    TopP = req.NucleusSampling,
                    PresencePenalty = req.PresencePenalty,
                    FrequencyPenalty = req.FrequencyPenalty,
                };
                return true;
            case ModelInfo.ModelProviders.OpenAI:
                settings = new OpenAIPromptExecutionSettings
                {
                    MaxTokens = req.MaxTokens, // note: nullable
                    Temperature = req.Temperature,
                    TopP = req.NucleusSampling,
                    PresencePenalty = req.PresencePenalty,
                    FrequencyPenalty = req.FrequencyPenalty,
                };
                return true;
            case ModelInfo.ModelProviders.Ollama:
                settings = new OllamaPromptExecutionSettings
                {
                    NumPredict = req.MaxTokens, // note: nullable
                    Temperature = req.Temperature,
                    TopP = req.NucleusSampling,
                };
                return true;
            default:
                error = Results.BadRequest($"Unknown model provider: {model.Provider}");
                return false;
        }
    }

    private static ChatHistory GetChatHistory(GenerateTextRequest req)
    {
        ChatHistory chatHistory = new();
        if (!string.IsNullOrWhiteSpace(req.SystemPrompt)) { chatHistory.AddSystemMessage(req.SystemPrompt); }

        if (!string.IsNullOrWhiteSpace(req.Prompt)) { chatHistory.AddUserMessage(req.Prompt); }

        return chatHistory;
    }
}
