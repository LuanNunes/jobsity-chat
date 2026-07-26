using System.Text.Json;

namespace com.jobsite.chat.Shared.Messaging;

// Single JSON contract shared by every publisher and consumer on the wire.
// Web defaults (camelCase) + case-insensitive property matching so producer/consumer never drift.
public static class MessagingJson
{
    public static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
}
