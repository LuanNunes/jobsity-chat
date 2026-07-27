using System.Text.Json;

namespace com.jobsite.chat.Shared.Messaging;

public static class MessagingJson
{
    public static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
}
