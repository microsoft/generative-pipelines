// Copyright (c) Microsoft. All rights reserved.

// work in progress

// using System.Text.Json;
// using Qdrant.Client.Grpc;
//
// namespace VectorStorageSk.SemanticKernel;
//
// public class QdrantFilterTranslator
// {
//     public static Filter ToFilter(string query)
//     {
//         JsonElement tree = JsonSerializer.Deserialize<JsonElement>(query);
//         return ToFilter(tree);
//     }
//
//     public static Filter ToFilter(JsonElement tree)
//     {
//         var filter = new Filter();
//         foreach (var node in tree.EnumerateObject())
//         {
//             filter.Must.Add(ToCondition(node));
//         }
//
//         return filter;
//     }
//
//     public static Condition ToCondition(JsonProperty node)
//     {
//         var condition = new Condition();
//         switch (node.Name)
//         {
//             case "$and":
//             {
//                 var filter = new Filter();
//
//                 // The value is a list of conditions
//                 if (node.Value.ValueKind == JsonValueKind.Array)
//                 {
//                     foreach (JsonElement andNode in node.Value.EnumerateArray())
//                     {
//                         Filter f = ToFilter(andNode);
//                         filter.Must.Add(f.Must);
//                     }
//                 }
//                 else
//                 {
//                     throw new ArgumentException("'$and' must point to an array of conditions");
//                 }
//
//                 condition.Filter = filter;
//                 break;
//             }
//             case "$or":
//             {
//                 var filter = new Filter();
//
//                 // The value is a list of conditions
//                 if (node.Value.ValueKind == JsonValueKind.Array)
//                 {
//                     foreach (JsonElement orNode in node.Value.EnumerateArray())
//                     {
//                         Filter f = ToFilter(orNode);
//                         filter.Should.Add(f.Must);
//                     }
//                 }
//                 else
//                 {
//                     throw new ArgumentException("'$and' must point to an array of conditions");
//                 }
//
//                 condition.Filter = filter;
//                 break;
//
//                 // var filter = new Filter();
//                 // filter.Should.Add(ToFilter(node));
//                 // condition.Filter = filter;
//                 // break;
//             }
//             case "$not":
//             {
//                 break;
//             }
//             default:
//             {
//                 condition.Field = new FieldCondition
//                 {
//                     Key = node.Name,
//                     Match = node.Value.ValueKind switch
//                     {
//                         JsonValueKind.False => new Match { Boolean = false },
//                         JsonValueKind.True => new Match { Boolean = true },
//                         JsonValueKind.String => new Match { Keyword = node.Value.ToString() },
//                         JsonValueKind.Number => node.Value.GetRawText().IndexOfAny(['.', ',']) < 0
//                             ? new Match { Integer = long.Parse(node.Value.ToString()) }
//                             : throw new ArgumentException("Equality is not supported for decimal numbers"),
//                         _ => throw new InvalidOperationException($"Unsupported filter value kind '{node.Value.ValueKind}'")
//                     },
//                 };
//                 break;
//             }
//         }
//
//         return condition;
//     }
// }

#pragma warning disable tmp
