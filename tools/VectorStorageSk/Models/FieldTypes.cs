// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum FieldTypes
{
    Undefined = 0,

    PrimaryKey = 1,
    Key = 1,
    Id = 1,

    Vector = 2,
    Embedding = 2,

    Text = 3,
    String = 3,

    Bool = 11,
    Boolean = 11,

    Int = 12,
    Integer = 12,
    Long = 12,

    Number = 13,
    Float = 13,
    Double = 13,

    Date = 14,
    Time = 14,
    DateTime = 14,
    Timestamp = 14,

    Object = 15,

    ListOfNumber = 20,
    ListOfText = 21,
    ListOfBoolean = 22,
}
