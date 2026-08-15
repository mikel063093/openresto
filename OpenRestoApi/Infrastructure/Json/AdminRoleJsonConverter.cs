using System.Text.Json;
using System.Text.Json.Serialization;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Infrastructure.Json;

public sealed class AdminRoleJsonConverter : JsonConverter<AdminRole>
{
    public const string InvalidRoleMessage = "Invalid role. Allowed values: SuperAdmin, BookingViewer, BookingEditor.";

    public override AdminRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? raw = reader.GetString();
            if (Enum.TryParse<AdminRole>(raw, ignoreCase: false, out AdminRole role) &&
                Enum.IsDefined(role))
            {
                return role;
            }

            throw new JsonException(InvalidRoleMessage);
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int numericRole) &&
            Enum.IsDefined(typeof(AdminRole), numericRole))
        {
            return (AdminRole)numericRole;
        }

        throw new JsonException(InvalidRoleMessage);
    }

    public override void Write(Utf8JsonWriter writer, AdminRole value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
