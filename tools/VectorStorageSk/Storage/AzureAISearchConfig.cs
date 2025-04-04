// Copyright (c) Microsoft. All rights reserved.

namespace VectorStorageSk.Storage;

internal sealed class AzureAISearchConfig
{
    public sealed class AzureAISearchDeploymentConfig
    {
        /// <summary>
        /// Azure AI Search resource endpoint URL
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Azure authentication type
        /// </summary>
        public AzureAuthTypes Auth { get; set; } = AzureAuthTypes.AzureIdentity;

        /// <summary>
        /// API key, required if Auth == ApiKey
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Optional custom auth tokens audience for sovereign clouds, when using Auth.AzureIdentity
        /// See https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/search/Azure.Search.Documents/src/SearchAudience.cs
        /// Examples:
        /// - "https://search.azure.com"
        /// - "https://search.azure.us"
        /// - "https://search.azure.cn"
        /// </summary>
        public string? AzureIdentityAudience { get; set; } = null;

        public bool IsValid(ILogger? log = null)
        {
            if (string.IsNullOrWhiteSpace(this.Endpoint))
            {
                log?.LogError("Azure AI Search endpoint is empty");
                return false;
            }

            if (!Uri.TryCreate(this.Endpoint, UriKind.Absolute, out var _))
            {
                log?.LogError("Azure AI Search endpoint is not a valid URI");
                return false;
            }

            if (this.Auth == AzureAuthTypes.ApiKey && string.IsNullOrWhiteSpace(this.ApiKey))
            {
                log?.LogError("Azure AI Search API key is empty");
                return false;
            }

            if (this.Auth == AzureAuthTypes.Unknown)
            {
                log?.LogError("Unknown Azure AI Search authentication type");
                return false;
            }

            log?.LogDebug("Azure AI Search configuration is valid");
            return true;
        }
    }

    /// <summary>
    /// List of Azure AI Search deployments, in case more than one is in use.
    /// Use "default" key to define the default deployment to use when unspecified.
    /// </summary>
    public Dictionary<string, AzureAISearchDeploymentConfig> Deployments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
