using System.Text.Json;

namespace BackendAPI.Utils
{
    public static class JsonSanitizer
    {
        public static string EnsureEventIds(string jsonString)
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString)!;

            if (root.TryGetProperty("events", out var eventsProp) && eventsProp.ValueKind == JsonValueKind.Array)
            {
                var eventsWithIds = eventsProp.EnumerateArray().Select(e => new Dictionary<string, object>
                {
                    ["id"] = e.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
                        ? idProp.GetString()
                        : Guid.NewGuid().ToString(),
                    ["year"] = e.GetProperty("year").GetInt32(),
                    ["month"] = e.GetProperty("month").ToString(),
                    ["day"] = e.GetProperty("day").ToString(),
                    ["title"] = e.GetProperty("title").GetString() ?? "",
                    ["content"] = e.GetProperty("content").GetString() ?? "",
                    ["eraName"] = e.GetProperty("eraName").GetString() ?? "",
                    ["tags"] = e.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToArray()
                }).ToList();

                jsonDict["events"] = eventsWithIds;
            }

            return JsonSerializer.Serialize(jsonDict);
        }

    }

}
