// Copyright (c) Microsoft. All rights reserved.

using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace VectorStorageSk.SemanticKernel;

public class ODataFilterTranslator
{
    public static Expression<Func<T, bool>>? BuildFilterExpression<T>(string? filterString, ILogger log)
    {
        if (string.IsNullOrWhiteSpace(filterString))
        {
            return null;
        }

        log.LogDebug("Building filter expression for: {FilterString}; target model: {DataType}", filterString, typeof(T).FullName);

        ODataConventionModelBuilder builder = new();

        // builder.EntityType<T>().Name = "TargetClass"; // rewrite for <T>
        var entityTypeConfiguration = builder.AddEntityType(typeof(T));
        entityTypeConfiguration.Name = "TargetClass";

        // builder.EntitySet<T>("MemoryRecords"); // rewrite for <T>
        var entitySetConfiguration = builder.AddEntityType(typeof(T));
        builder.AddEntitySet("MemoryRecords", entitySetConfiguration);

        IEdmModel model = builder.GetEdmModel();

        IEdmEntityType entityType = model.SchemaElements.OfType<IEdmEntityType>().First(e => e.Name == "TargetClass");
        IEdmEntityContainer container = model.EntityContainer;
        IEdmEntitySet entitySet = container.FindEntitySet("MemoryRecords");
        Dictionary<string, string> queryOptions = new() { { "$filter", filterString } };
        ODataQueryOptionParser parser = new(model, entityType, entitySet, queryOptions);

        ODataPath path = new();
        ODataQueryContext context = new(model, typeof(T), path);
        FilterQueryOption filter = new(filterString, context, parser);

        QueryBinderContext binderContext = new(model, new ODataQuerySettings
        {
            EnableConstantParameterization = false,
        }, typeof(T));

        Expression? expression = new FilterBinder().BindFilter(filter.FilterClause, binderContext);
        log.LogDebug("Filter expression: {Expression}", expression);

        Expression<Func<T, bool>>? lambda = expression as Expression<Func<T, bool>>;
        log.LogDebug("Filter lambda: {Lambda}", lambda);

        if (lambda == null)
        {
            throw new InvalidOperationException($"Unable to convert filter expression to lambda: {filterString}");
        }

        // return lambda;

        var visitor = new RemoveRedundantConvertVisitor();
        var cleaned = (Expression<Func<T, bool>>)visitor.Visit(lambda);
        log.LogDebug("Clean lambda: {Lambda}", cleaned);

        return cleaned;
    }
}

internal sealed class RemoveRedundantConvertVisitor : ExpressionVisitor
{
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert)
        {
            var operand = this.Visit(node.Operand);

            // If conversion is unnecessary (e.g., string to string), skip it
            // if (operand.Type == node.Type)
            // {
            return operand;
            // }

            // return Expression.Convert(operand, node.Type);
        }

        return base.VisitUnary(node);
    }
}
