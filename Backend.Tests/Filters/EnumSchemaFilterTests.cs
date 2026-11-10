using Backend.Filters;
using Backend.Models.Domain;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;
using NSubstitute;

namespace Backend.Tests.Filters;

public class EnumSchemaFilterTests
{
    [Fact]
    public void Apply_WhenTypeIsEnum_ModifiesSchemaToStringAndListsNames()
    {
        // Arrange
        var filter = new EnumSchemaFilter();
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Integer,
            Format = "int32",
            Enum = [JsonValue.Create(0), JsonValue.Create(1)]
        };

        var schemaGeneratorMock = Substitute.For<ISchemaGenerator>();
        var schemaRepository = new SchemaRepository();

        var context = new SchemaFilterContext(
            type: typeof(StudyStatus),
            schemaGenerator: schemaGeneratorMock,
            schemaRepository: schemaRepository
        );

        // Act
        filter.Apply(schema, context);

        // Assert
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.NotNull(schema.Enum);
        Assert.Equal(3, schema.Enum.Count); // Enrolled, Completed, DroppedOut
        Assert.Equal("Enrolled", schema.Enum[0].GetValue<string>());
    }

    [Fact]
    public void Apply_WhenTypeIsNotEnum_DoesNotModifySchema()
    {
        // Arrange
        var filter = new EnumSchemaFilter();
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object
        };

        var schemaGeneratorMock = Substitute.For<ISchemaGenerator>();
        var schemaRepository = new SchemaRepository();

        var context = new SchemaFilterContext(
            type: typeof(Member),
            schemaGenerator: schemaGeneratorMock,
            schemaRepository: schemaRepository
        );

        // Act
        filter.Apply(schema, context);

        // Assert
        Assert.Equal(JsonSchemaType.Object, schema.Type);
        Assert.Null(schema.Enum);
    }
}
