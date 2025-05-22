using OpenAI.Chat;
using System.Text.Json;
using ZvezdoGpt.WebApi.Dtos;
using ChatMessageContentPart = ZvezdoGpt.WebApi.Dtos.ChatMessageContentPart;

internal class ChatCompletionService(ChatClient chatClient)
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async IAsyncEnumerable<string> Complete(IEnumerable<ChatMessageDto> messageDtos)
    {
        var messages = messageDtos
            .Where(dto => dto.Role is "user" or "assistant")
            .Where(dto => dto.Content.ValueKind is JsonValueKind.String or JsonValueKind.Array)
            .Select(ToChatMessage)
            .ToList();

        await foreach (var completionUpdate in chatClient.CompleteChatStreamingAsync(messages))
        {
            var responseContent = string.Concat(completionUpdate.ContentUpdate.Where(x => x.Kind is ChatMessageContentPartKind.Text).Select(x => x.Text));

            if (!string.IsNullOrEmpty(responseContent))
            {
                var payload = new
                {
                    id = Guid.NewGuid().ToString(),
                    @object = "chat.completion.chunk",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    choices = new[] { new { delta = new { content = responseContent } } }
                };

                yield return JsonSerializer.Serialize(payload);
            }
        }
    }

    private static ChatMessage ToChatMessage(ChatMessageDto dto)
    {
        string content;
        if (dto.Content.ValueKind is JsonValueKind.String)
        {
            content = dto.Content.ToString();
        }
        else
        {
            var parts = JsonSerializer.Deserialize<ChatMessageContentPart[]>(dto.Content.GetRawText(), jsonSerializerOptions);
            content = string.Join(" ", parts.Where(p => p.Type == "text").Select(p => p.Text));
        }

        return dto.Role == "user" ? new UserChatMessage(content) : new AssistantChatMessage(content);
    }
}