// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace TextGenerator.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum AuthTypes
{
    Unknown = -1,

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
