using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Filters;

/// <summary>
/// The EnumSchemaFilter class is a custom schema filter for Swagger/OpenAPI documentation that modifies the way enum types are represented in the generated API documentation. When applied, this filter changes the schema of enum types to be represented as strings instead of their underlying integer values. It iterates through the names of the enum members and adds them as string values to the schema's Enum collection. This enhances the readability and usability of the API documentation by providing more descriptive representations of enum values, making it easier for developers to understand and use the API effectively when enums are involved in request or response models.
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Applies the schema filter to modify the representation of enum types in the Swagger/OpenAPI documentation. This method checks if the type being processed is an enum, and if so, it changes the schema type to "string" and populates the Enum collection with the names of the enum members as string values. This allows for a more user-friendly representation of enums in the API documentation, improving clarity and usability for developers who interact with the API.
    /// </summary>
    /// <param name="schema">The OpenAPI schema to modify.</param>
    /// <param name="context">The schema filter context.</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Type = "string";
            schema.Enum.Clear();
            foreach (var name in Enum.GetNames(context.Type))
            {
                schema.Enum.Add(new OpenApiString(name));
            }
        }
    }
}
