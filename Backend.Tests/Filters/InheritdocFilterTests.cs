using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Backend.Filters;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NSubstitute;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Backend.Tests.Filters;

public class InheritdocFilterTests : IDisposable
{
    private readonly string _tempXmlPath;

    public InheritdocFilterTests()
    {
        _tempXmlPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");

        var xmlContent = @"
<doc>
  <members>
    <member name=""T:Backend.Models.Domain.Setting"">
      <summary>This is a setting summary.</summary>
    </member>
    <member name=""P:Backend.Models.Domain.Setting.Name"">
      <summary>The setting name.</summary>
    </member>
    <member name=""P:Backend.Models.Domain.Setting.Value"">
      <inheritdoc cref=""P:Backend.Models.Domain.Setting.Name""/>
    </member>
    <member name=""P:Backend.Tests.Filters.InheritdocFilterTests.TestModel.SelfReferential"">
      <inheritdoc cref=""P:Backend.Tests.Filters.InheritdocFilterTests.TestModel.SelfReferential""/>
    </member>
    <member name=""P:Backend.Tests.Filters.InheritdocFilterTests.TestModel.SystemJsonProperty"">
      <summary>System Json Property.</summary>
    </member>
    <member name=""P:Backend.Tests.Filters.InheritdocFilterTests.TestModel.NewtonsoftJsonProperty"">
      <summary>Newtonsoft Json Property.</summary>
    </member>
    <member name=""P:Backend.Tests.Filters.InheritdocFilterTests.TestModel.CamelCaseProperty"">
      <summary>Camel Case Property.</summary>
    </member>
  </members>
</doc>";

        File.WriteAllText(_tempXmlPath, xmlContent);
    }

    public void Dispose()
    {
        if (File.Exists(_tempXmlPath))
        {
            File.Delete(_tempXmlPath);
        }
    }

    public class TestModel
    {
        public string? SelfReferential { get; set; }

        [JsonPropertyName("special_system_name")]
        public string? SystemJsonProperty { get; set; }

        [JsonProperty("special_newtonsoft_name")]
        public string? NewtonsoftJsonProperty { get; set; }

        public string? CamelCaseProperty { get; set; }
    }

    public class GenericModel<T>
    {
        public T? Value { get; set; }
    }

    [Fact]
    public void InheritdocXmlComments_NonExistentPath_ReturnsEmptyComments()
    {
        var comments = InheritdocXmlComments.GetOrCreate("nonexistent.xml");
        var summary = comments.GetSummary("T:Backend.Models.Domain.Setting");

        Assert.Null(summary);
    }

    [Fact]
    public void InheritdocXmlComments_GetSummary_HandlesInheritdoc()
    {
        var comments = InheritdocXmlComments.GetOrCreate(_tempXmlPath);

        var typeSummary = comments.GetSummary("T:Backend.Models.Domain.Setting");
        var nameSummary = comments.GetSummary("P:Backend.Models.Domain.Setting.Name");
        var valueSummary = comments.GetSummary("P:Backend.Models.Domain.Setting.Value");

        Assert.Equal("This is a setting summary.", typeSummary);
        Assert.Equal("The setting name.", nameSummary);
        Assert.Equal("The setting name.", valueSummary);
    }

    [Fact]
    public void InheritdocXmlComments_GetSummary_SelfReferentialInheritdoc_ReturnsNull()
    {
        var comments = InheritdocXmlComments.GetOrCreate(_tempXmlPath);
        var summary = comments.GetSummary("P:Backend.Tests.Filters.InheritdocFilterTests.TestModel.SelfReferential");

        Assert.Null(summary);
    }

    [Fact]
    public void InheritdocXmlComments_GetMemberName_GenericType_Works()
    {
        var name = InheritdocXmlComments.GetMemberName(typeof(GenericModel<string>));
        Assert.StartsWith("T:Backend.Tests.Filters.InheritdocFilterTests.GenericModel`1", name);
    }

    [Fact]
    public void InheritdocXmlComments_GetMemberName_GenericProperty_Works()
    {
        var propertyInfo = typeof(GenericModel<string>).GetProperty("Value")!;
        var name = InheritdocXmlComments.GetMemberName(propertyInfo);
        Assert.StartsWith("P:Backend.Tests.Filters.InheritdocFilterTests.GenericModel`1.Value", name);
    }

    [Fact]
    public void InheritdocSchemaFilter_Apply_SetsDescriptionAndProperties()
    {
        var filter = new InheritdocSchemaFilter(_tempXmlPath);
        var schema = new OpenApiSchema
        {
            Description = ""
        };
        schema.Properties.Add("Name", new OpenApiSchema { Description = "" });
        schema.Properties.Add("Value", new OpenApiSchema { Description = "" });

        var context = new SchemaFilterContext(
            type: typeof(Setting),
            schemaGenerator: Substitute.For<ISchemaGenerator>(),
            schemaRepository: new SchemaRepository()
        );

        filter.Apply(schema, context);

        Assert.Equal("This is a setting summary.", schema.Description);
        Assert.Equal("The setting name.", schema.Properties["Name"].Description);
        Assert.Equal("The setting name.", schema.Properties["Value"].Description);
    }

    [Fact]
    public void InheritdocSchemaFilter_Apply_ResolvesDifferentPropertyNames()
    {
        var filter = new InheritdocSchemaFilter(_tempXmlPath);
        var schema = new OpenApiSchema { Description = "" };
        schema.Properties.Add("special_system_name", new OpenApiSchema { Description = "" });
        schema.Properties.Add("special_newtonsoft_name", new OpenApiSchema { Description = "" });
        schema.Properties.Add("camelCaseProperty", new OpenApiSchema { Description = "" });

        var context = new SchemaFilterContext(
            type: typeof(TestModel),
            schemaGenerator: Substitute.For<ISchemaGenerator>(),
            schemaRepository: new SchemaRepository()
        );

        filter.Apply(schema, context);

        Assert.Equal("System Json Property.", schema.Properties["special_system_name"].Description);
        Assert.Equal("Newtonsoft Json Property.", schema.Properties["special_newtonsoft_name"].Description);
        Assert.Equal("Camel Case Property.", schema.Properties["camelCaseProperty"].Description);
    }

    private class TestController
    {
        public void ActionMethod([FromForm] Setting formModel) { }
        public void NonFormAction(Setting model) { }
        public void ActionWithTestModel([FromForm] TestModel model) { }
    }

    [Fact]
    public void FormRequestBodyInheritdocFilter_Apply_SetsMultipartDescriptions()
    {
        var filter = new FormRequestBodyInheritdocFilter(_tempXmlPath);
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };

        var schema = new OpenApiSchema
        {
            Description = ""
        };
        schema.Properties.Add("Name", new OpenApiSchema { Description = "" });

        operation.RequestBody.Content.Add("multipart/form-data", new OpenApiMediaType
        {
            Schema = schema
        });

        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        var context = new OperationFilterContext(
            null!,
            Substitute.For<ISchemaGenerator>(),
            new SchemaRepository(),
            methodInfo
        );

        filter.Apply(operation, context);

        Assert.Equal("This is a setting summary.", schema.Description);
        Assert.Equal("The setting name.", schema.Properties["Name"].Description);
    }

    [Fact]
    public void FormRequestBodyInheritdocFilter_Apply_NoFormAttribute_ReturnsEarly()
    {
        var filter = new FormRequestBodyInheritdocFilter(_tempXmlPath);
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };

        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.NonFormAction));
        var context = new OperationFilterContext(
            null!,
            Substitute.For<ISchemaGenerator>(),
            new SchemaRepository(),
            methodInfo
        );

        filter.Apply(operation, context);

        Assert.Empty(operation.RequestBody.Content);
    }

    [Fact]
    public void FormRequestBodyInheritdocFilter_Apply_ResolvesDifferentPropertyNames()
    {
        var filter = new FormRequestBodyInheritdocFilter(_tempXmlPath);
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };

        var schema = new OpenApiSchema { Description = "" };
        schema.Properties.Add("special_system_name", new OpenApiSchema { Description = "" });
        schema.Properties.Add("special_newtonsoft_name", new OpenApiSchema { Description = "" });
        schema.Properties.Add("camelCaseProperty", new OpenApiSchema { Description = "" });

        operation.RequestBody.Content.Add("application/x-www-form-urlencoded", new OpenApiMediaType
        {
            Schema = schema
        });

        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionWithTestModel));
        var context = new OperationFilterContext(
            null!,
            Substitute.For<ISchemaGenerator>(),
            new SchemaRepository(),
            methodInfo
        );

        filter.Apply(operation, context);

        Assert.Equal("System Json Property.", schema.Properties["special_system_name"].Description);
        Assert.Equal("Newtonsoft Json Property.", schema.Properties["special_newtonsoft_name"].Description);
        Assert.Equal("Camel Case Property.", schema.Properties["camelCaseProperty"].Description);
    }

    [Fact]
    public void InheritdocXmlComments_GetMemberName_GenericParameter_ReturnsNull()
    {
        var type = typeof(GenericModel<>).GetGenericArguments()[0];
        var name = InheritdocXmlComments.GetMemberName(type);
        Assert.Null(name);
    }

    [Fact]
    public void InheritdocSchemaFilter_Apply_NullSchema_ReturnsEarly()
    {
        var filter = new InheritdocSchemaFilter(_tempXmlPath);
        var context = new SchemaFilterContext(typeof(Setting), Substitute.For<ISchemaGenerator>(), new SchemaRepository());
        filter.Apply(null!, context);
    }

    [Fact]
    public void InheritdocSchemaFilter_Apply_ExistingDescription_DoesNotOverwrite()
    {
        var filter = new InheritdocSchemaFilter(_tempXmlPath);
        var schema = new OpenApiSchema { Description = "Existing summary" };
        var context = new SchemaFilterContext(typeof(Setting), Substitute.For<ISchemaGenerator>(), new SchemaRepository());
        filter.Apply(schema, context);
        Assert.Equal("Existing summary", schema.Description);
    }

    [Fact]
    public void InheritdocSchemaFilter_Apply_NullProperties_ReturnsEarly()
    {
        var filter = new InheritdocSchemaFilter(_tempXmlPath);
        var schema = new OpenApiSchema { Description = "", Properties = null! };
        var context = new SchemaFilterContext(typeof(Setting), Substitute.For<ISchemaGenerator>(), new SchemaRepository());
        filter.Apply(schema, context);
    }

    [Fact]
    public void FormRequestBodyInheritdocFilter_Apply_NullRequestBody_ReturnsEarly()
    {
        var filter = new FormRequestBodyInheritdocFilter(_tempXmlPath);
        var operation = new OpenApiOperation { RequestBody = null };
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        var context = new OperationFilterContext(null!, Substitute.For<ISchemaGenerator>(), new SchemaRepository(), methodInfo);
        filter.Apply(operation, context);
    }

    [Fact]
    public void FormRequestBodyInheritdocFilter_Apply_UnsupportedContentType_Skips()
    {
        var filter = new FormRequestBodyInheritdocFilter(_tempXmlPath);
        var operation = new OpenApiOperation { RequestBody = new OpenApiRequestBody() };
        var schema = new OpenApiSchema { Description = "" };
        operation.RequestBody.Content.Add("application/json", new OpenApiMediaType { Schema = schema });
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        var context = new OperationFilterContext(null!, Substitute.For<ISchemaGenerator>(), new SchemaRepository(), methodInfo);
        filter.Apply(operation, context);
        Assert.Equal("", schema.Description);
    }

    [Fact]
    public void FormRequestBodyInheritdocFilter_Apply_NullProperties_Skips()
    {
        var filter = new FormRequestBodyInheritdocFilter(_tempXmlPath);
        var operation = new OpenApiOperation { RequestBody = new OpenApiRequestBody() };
        var schema = new OpenApiSchema { Description = "", Properties = null! };
        operation.RequestBody.Content.Add("multipart/form-data", new OpenApiMediaType { Schema = schema });
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        var context = new OperationFilterContext(null!, Substitute.For<ISchemaGenerator>(), new SchemaRepository(), methodInfo);
        filter.Apply(operation, context);
    }

    [Fact]
    public void FormRequestBodyInheritdocFilter_Apply_ExistingDescription_DoesNotOverwrite()
    {
        var filter = new FormRequestBodyInheritdocFilter(_tempXmlPath);
        var operation = new OpenApiOperation { RequestBody = new OpenApiRequestBody() };
        var schema = new OpenApiSchema { Description = "Existing summary" };
        operation.RequestBody.Content.Add("multipart/form-data", new OpenApiMediaType { Schema = schema });
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        var context = new OperationFilterContext(null!, Substitute.For<ISchemaGenerator>(), new SchemaRepository(), methodInfo);
        filter.Apply(operation, context);
        Assert.Equal("Existing summary", schema.Description);
    }

    [Fact]
    public void InheritdocSchemaFilter_Apply_EdgeCases()
    {
        var filter = new InheritdocSchemaFilter(_tempXmlPath);
        var schema = new OpenApiSchema { Description = "" };

        schema.Properties.Add("special_system_name", new OpenApiSchema { Description = "Existing Desc" });
        schema.Properties.Add("CAMELCASEPROPERTY", new OpenApiSchema { Description = "" });

        var context = new SchemaFilterContext(
            type: typeof(TestModel),
            schemaGenerator: Substitute.For<ISchemaGenerator>(),
            schemaRepository: new SchemaRepository()
        );

        filter.Apply(schema, context);

        Assert.Equal("Existing Desc", schema.Properties["special_system_name"].Description);
        Assert.Equal("Camel Case Property.", schema.Properties["CAMELCASEPROPERTY"].Description);
    }
}
