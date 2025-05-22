using System.Text.Json;

namespace ZvezdoGpt.WebApi.Dtos;

internal class ChatMessageDto
{
    public string Role { get; set; }

    public JsonElement Content { get; set; }
}