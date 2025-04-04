// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Storage;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AzureAuthTypes
{
    Unknown = -1,

    // AzureIdentity: use automatic Entra (AAD) authentication mechanism.
    //   When the service is on sovereign clouds you can use the AZURE_AUTHORITY_HOST env var to
    //   set the authority host. See https://learn.microsoft.com/dotnet/api/overview/azure/identity-readme
    //   You can test locally using the AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET env vars.
    AzureIdentity,

    ApiKey,
}
