// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace EmbeddingGenerator.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthTypes
{
    ApiKey,

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
