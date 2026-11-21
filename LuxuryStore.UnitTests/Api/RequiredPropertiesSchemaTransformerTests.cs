using LuxuryStore_Backend.Contract;
using Microsoft.OpenApi;

namespace LuxuryStore.UnitTests.Api;

public class RequiredPropertiesSchemaTransformerTests
{
    private static OpenApiSchema Apply(Dictionary<string, IOpenApiSchema> properties)
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Object, Properties = properties };
        RequiredPropertiesSchemaTransformer.Apply(schema);
        return schema;
    }

    [Fact]
    public void Properties_that_cannot_be_null_become_required()
    {
        var schema = Apply(new()
        {
            ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["basePrice"] = new OpenApiSchema { Type = JsonSchemaType.Number },
        });

        schema.Required.ShouldBe(["name", "basePrice"], ignoreOrder: true);
    }

    [Fact]
    public void Nullable_properties_stay_optional()
    {
        var schema = Apply(new()
        {
            ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["description"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["brand"] = new OpenApiSchema
            {
                OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, new OpenApiSchema { Type = JsonSchemaType.Object }],
            },
        });

        schema.Required.ShouldBe(["name"]);
    }

    [Fact]
    public void Schemas_without_properties_are_left_alone()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };

        RequiredPropertiesSchemaTransformer.Apply(schema);

        schema.Required.ShouldBeNull();
    }
}
