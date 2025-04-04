// Copyright (c) Microsoft. All rights reserved.

// namespace VectorStorageSk.Tests;
//
// public sealed class XTest : BaseTestCase
// {
//     public XTest(ITestOutputHelper console) : base(console)
//     {
//     }
//
//     [Fact]
//     public void ItDoes()
//     {
//         // var query = """
//         //             { "isActive": true, "age": 5, "name": "John" }
//         //             """;
//         var query = """
//                     {
//                         "$and": [
//                           { "year": 2025 },
//                           { "age": 23 },
//                           {
//                             "$or": [
//                               { "country": "US" },
//                               { "country": "CA" }
//                             ]
//                           }
//                         ]
//                     }
//                     """;
//
//         var filter = QdrantFilterTranslator.ToFilter(query);
//         Console.WriteLine(filter.ToString());
//     }
// }

#pragma warning disable tmp
