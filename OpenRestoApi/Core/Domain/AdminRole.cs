using System.Text.Json.Serialization;
using OpenRestoApi.Infrastructure.Json;

namespace OpenRestoApi.Core.Domain;

[JsonConverter(typeof(AdminRoleJsonConverter))]
public enum AdminRole
{
    SuperAdmin,
    BookingViewer,
    BookingEditor,
}
