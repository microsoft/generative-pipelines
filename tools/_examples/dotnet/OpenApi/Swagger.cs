// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Scalar.AspNetCore;

// TEMPLATE: set a real namespace, it's used below
namespace dotnetTemplate.OpenApi;

internal static class Swagger
{
    public static void AddOpenApiDevTools(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(o => { o.SwaggerEndpoint("/openapi/v1.json", "v1"); });
            app.MapScalarApiReference(options =>
            {
                // options.DarkMode = false; options.Theme = ScalarTheme.Alternate;
                // options.DarkMode = false; options.Theme = ScalarTheme.Moon;
                // options.DarkMode = true; options.Theme = ScalarTheme.Solarized;
                options.DarkMode = true;
                options.Theme = ScalarTheme.Solarized;

                // options.Layout = ScalarLayout.Classic;
                options.Layout = ScalarLayout.Modern;
            });
            app.MapGet("/", () =>
            {
                string html = $"""
                               <html><head><link href="https://fonts.googleapis.com/css2?family=Open+Sans:wght@400;600&display=swap" rel="stylesheet"></head>
                               <body style='font-family:Open Sans'><ul style='list-style-position:inside'>
                               <h1>{MethodBase.GetCurrentMethod()?.DeclaringType?.Namespace}</h1>
                                   <li><a href="/swagger">Swagger UI</a></li>
                                   <li><a href="/scalar">Scalar API Reference</a></li>
                               </ul></body></html>
                               """;
                return Results.Content(html, "text/html");
            });
        }
    }
}
