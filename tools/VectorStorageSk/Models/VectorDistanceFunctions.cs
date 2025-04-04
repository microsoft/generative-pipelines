// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum VectorDistanceFunctions
{
    Undefined = 0,
    CosineSimilarity,
    CosineDistance,
    DotProductSimilarity,
    NegativeDotProductSimilarity,
    EuclideanDistance,
    EuclideanSquaredDistance,
    Hamming,
    ManhattanDistance,
}
