using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Backend.Filters;

/// <summary>
/// Applies inherited XML summaries to multipart form request body schemas.
/// </summary>
public sealed class FormRequestBodyInheritdocFilter : IOperationFilter
{
    private readonly InheritdocXmlComments _comments;

    /// <summary>
    /// Initializes the filter using the provided XML documentation file.
    /// </summary>
    /// <param name="xmlPath">The XML documentation file path.</param>
    public FormRequestBodyInheritdocFilter(string xmlPath)
    {
        _comments = InheritdocXmlComments.GetOrCreate(xmlPath);
    }

    /// <summary>
    /// Applies inherited summaries to multipart/form-data request schemas.
    /// </summary>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <param name="context">The operation filter context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content == null)
            return;

        var formParameter = context.MethodInfo.GetParameters()
            .FirstOrDefault(parameter => parameter.GetCustomAttribute<FromFormAttribute>() != null);

        if (formParameter == null)
            return;

        var modelType = formParameter.ParameterType;

        foreach (var content in operation.RequestBody.Content)
        {
            if (!string.Equals(content.Key, "multipart/form-data", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(content.Key, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var schema = content.Value.Schema;
            if (schema?.Properties == null || schema.Properties.Count == 0)
                continue;

            if (string.IsNullOrWhiteSpace(schema.Description))
            {
                var typeMemberName = InheritdocXmlComments.GetMemberName(modelType);
                if (typeMemberName != null)
                {
                    var summary = _comments.GetSummary(typeMemberName);
                    if (!string.IsNullOrWhiteSpace(summary))
                        schema.Description = summary;
                }
            }

            foreach (var property in modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var memberName = InheritdocXmlComments.GetMemberName(property);
                if (memberName == null)
                    continue;

                var summary = _comments.GetSummary(memberName);
                if (string.IsNullOrWhiteSpace(summary))
                    continue;

                var propertySchema = ResolveSchemaProperty(schema, property);
                if (propertySchema != null && string.IsNullOrWhiteSpace(propertySchema.Description))
                    propertySchema.Description = summary;
            }
        }
    }

    private static OpenApiSchema? ResolveSchemaProperty(OpenApiSchema schema, PropertyInfo property)
    {
        var jsonNameAttribute = property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
        if (!string.IsNullOrWhiteSpace(jsonNameAttribute?.Name) &&
            schema.Properties.TryGetValue(jsonNameAttribute.Name, out var jsonPropertySchema))
        {
            return jsonPropertySchema;
        }

        var newtonsoftNameAttribute = property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>();
        if (!string.IsNullOrWhiteSpace(newtonsoftNameAttribute?.PropertyName) &&
            schema.Properties.TryGetValue(newtonsoftNameAttribute.PropertyName, out var newtonsoftPropertySchema))
        {
            return newtonsoftPropertySchema;
        }

        if (schema.Properties.TryGetValue(property.Name, out var pascalSchema))
            return pascalSchema;

        var camelName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        if (schema.Properties.TryGetValue(camelName, out var camelSchema))
            return camelSchema;

        return schema.Properties
            .FirstOrDefault(entry => string.Equals(entry.Key, property.Name, StringComparison.OrdinalIgnoreCase))
            .Value;
    }
}
