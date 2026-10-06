using Backend.Filters;
using Backend.Models.Domain;
using Microsoft.OpenApi.Models;
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
            Type = "integer",
            Format = "int32"
        };
        schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiInteger(0));
        schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiInteger(1));

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
        Assert.Equal("string", schema.Type);
        Assert.Equal(3, schema.Enum.Count); // Enrolled, Completed, DroppedOut
        var firstVal = (Microsoft.OpenApi.Any.OpenApiString)schema.Enum[0];
        Assert.Equal("Enrolled", firstVal.Value);
    }

    [Fact]
    public void Apply_WhenTypeIsNotEnum_DoesNotModifySchema()
    {
        // Arrange
        var filter = new EnumSchemaFilter();
        var schema = new OpenApiSchema
        {
            Type = "object"
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
        Assert.Equal("object", schema.Type);
        Assert.Empty(schema.Enum);
    }
}
