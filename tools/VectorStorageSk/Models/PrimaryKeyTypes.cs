// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum PrimaryKeyTypes
{
    Default = 0,
    String = 1,
    Guid = 2,
    Number = 3,
}
