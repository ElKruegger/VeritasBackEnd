using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LuxuryStore_Backend.Contract;

/// <summary>
/// Marks every property that can't be null as required in the OpenAPI
/// document. The API always writes every property, but C# classes don't say
/// so, and the generator then lists them all as optional — the frontend's
/// generated types would read "name?: string" for a name that is always
/// there. Nullable properties stay optional.
/// </summary>
public sealed class RequiredPropertiesSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        Apply(schema);
        return Task.CompletedTask;
    }

    public static void Apply(OpenApiSchema schema)
    {
        if (schema.Properties is null) return;

        foreach (var (name, property) in schema.Properties)
        {
            if (IsNullable(property)) continue;
            schema.Required ??= new HashSet<string>();
            schema.Required.Add(name);
        }
    }

    // "type": ["null", "string"] for primitives; a nullable object is a
    // oneOf/anyOf that includes { "type": "null" }.
    private static bool IsNullable(IOpenApiSchema property) =>
        HasNull(property)
        || (property.OneOf?.Any(HasNull) ?? false)
        || (property.AnyOf?.Any(HasNull) ?? false);

    private static bool HasNull(IOpenApiSchema schema) =>
        schema.Type is { } type && type.HasFlag(JsonSchemaType.Null);
}
