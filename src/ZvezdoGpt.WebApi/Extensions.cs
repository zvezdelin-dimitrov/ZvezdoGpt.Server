using OpenAI.Chat;
using System.Text.Json;
using ZvezdoGpt.WebApi.Dtos;

internal static class Extensions
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEnumerable<ChatMessage> ToChatMessages(this IEnumerable<ChatMessageDto> messageDtos)
    {
        return messageDtos
            .Where(dto => dto.Role is "user" or "assistant")
            .Where(dto => dto.Content.ValueKind is JsonValueKind.String or JsonValueKind.Array)
            .Select(ToChatMessage);
    }

    private static ChatMessage ToChatMessage(this ChatMessageDto dto)
    {
        string content;
        if (dto.Content.ValueKind is JsonValueKind.String)
        {
            content = dto.Content.ToString();
        }
        else
        {
            var parts = JsonSerializer.Deserialize<ChatMessageContentPart[]>(dto.Content.GetRawText(), jsonSerializerOptions);
            content = string.Join(string.Empty, parts.Where(p => p.Type == "text").Select(p => p.Text));
        }

        return dto.Role == "user" ? new UserChatMessage(content) : new AssistantChatMessage(content);
    }

    private class ChatMessageContentPart
    {
        public string Type { get; set; }

        public string Text { get; set; }
    }
}
