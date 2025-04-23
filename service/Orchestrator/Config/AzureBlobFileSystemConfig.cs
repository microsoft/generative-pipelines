// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Orchestrator.Diagnostics;

namespace Orchestrator.Config;

public sealed class AzureBlobFileSystemConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AzureAuthTypes
    {
        Unknown = -1,

        // Use this to default to .NET Aspire settings
        ConnectionString,

        // Use this to loop through multiple auth methods (slow)
        DefaultAzureCredential,

        // Use this if you auth with `az login` on your workstation
        AzureCliCredential,

        AzureDeveloperCliCredential,
        AzurePowerShellCredential,
        EnvironmentCredential,
        InteractiveBrowserCredential,
        ManagedIdentityCredential,
        VisualStudioCodeCredential,
        VisualStudioCredential,
    }

    /// <summary>
    /// Azure authentication type
    /// Default to .NET Aspire mode
    /// </summary>
    public AzureAuthTypes Auth { get; set; } = AzureAuthTypes.ConnectionString;

    /// <summary>
    /// Container used for storing files
    /// </summary>
    public string Container { get; set; } = "";

    /// <summary>
    /// Whether to lock blobs with a lease while writing.
    /// Slows down the orchestrator but prevents other processes (if any) from causing inconsistencies.
    /// </summary>
    public bool LeaseBlobs { get; set; } = false;

    public AzureBlobFileSystemConfig Validate()
    {
        if (string.IsNullOrWhiteSpace(this.ToString()))
        {
            throw new ConfigurationException("AzureBlobFileSystemConfig: Container name is empty");
        }

        return this;
    }
}
