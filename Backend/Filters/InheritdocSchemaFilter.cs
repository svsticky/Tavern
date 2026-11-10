using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json;

namespace Backend.Filters;

/// <summary>
/// Ensures Swagger schema descriptions are filled from XML documentation, including inheritdoc references.
/// </summary>
public sealed class InheritdocSchemaFilter : ISchemaFilter
{
    private readonly InheritdocXmlComments _comments;

    /// <summary>
    /// Initializes the filter using the provided XML documentation file.
    /// </summary>
    /// <param name="xmlPath">The XML documentation file path.</param>
    public InheritdocSchemaFilter(string xmlPath)
    {
        _comments = InheritdocXmlComments.GetOrCreate(xmlPath);
    }

    /// <summary>
    /// Applies inherited XML summaries to schema and property descriptions when they are missing.
    /// </summary>
    /// <param name="schema">The schema being generated.</param>
    /// <param name="context">The schema filter context.</param>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema == null)
            return;

        if (string.IsNullOrWhiteSpace(schema.Description))
        {
            var typeMemberName = InheritdocXmlComments.GetMemberName(context.Type);
            if (typeMemberName != null)
            {
                var summary = _comments.GetSummary(typeMemberName);
                if (!string.IsNullOrWhiteSpace(summary))
                    schema.Description = summary;
            }
        }

        if (schema.Properties == null || schema.Properties.Count == 0)
            return;

        foreach (var property in context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var memberName = InheritdocXmlComments.GetMemberName(property);
            if (memberName == null)
                continue;

            var summary = _comments.GetSummary(memberName);
            if (string.IsNullOrWhiteSpace(summary))
                continue;

            var schemaProperty = ResolveSchemaProperty(schema, property);
            if (schemaProperty != null && string.IsNullOrWhiteSpace(schemaProperty.Description))
                schemaProperty.Description = summary;
        }
    }

    private static IOpenApiSchema? ResolveSchemaProperty(IOpenApiSchema schema, PropertyInfo property)
    {
        var properties = schema.Properties;
        if (properties == null)
            return null;

        var jsonNameAttribute = property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
        if (!string.IsNullOrWhiteSpace(jsonNameAttribute?.Name) &&
            properties.TryGetValue(jsonNameAttribute.Name, out var jsonPropertySchema))
        {
            return jsonPropertySchema;
        }

        var newtonsoftNameAttribute = property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>();
        if (!string.IsNullOrWhiteSpace(newtonsoftNameAttribute?.PropertyName) &&
            properties.TryGetValue(newtonsoftNameAttribute.PropertyName, out var newtonsoftPropertySchema))
        {
            return newtonsoftPropertySchema;
        }

        if (properties.TryGetValue(property.Name, out var pascalSchema))
            return pascalSchema;

        var camelName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        if (properties.TryGetValue(camelName, out var camelSchema))
            return camelSchema;

        return properties
            .FirstOrDefault(entry => string.Equals(entry.Key, property.Name, StringComparison.OrdinalIgnoreCase))
            .Value;
    }
}
