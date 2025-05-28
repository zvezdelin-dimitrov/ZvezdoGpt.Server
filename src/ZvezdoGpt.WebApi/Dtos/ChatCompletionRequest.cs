namespace ZvezdoGpt.WebApi.Dtos;

internal class ChatCompletionRequest
{
    public string Model { get; set; }

    public List<ChatMessageDto> Messages { get; set; }

    public bool Stream { get; set; }

    public double? Temperature { get; set; }

    public double? Top_P { get; set; }

    public int? MaxTokens { get; set; }

    public string ApiKey { get; set; }
}