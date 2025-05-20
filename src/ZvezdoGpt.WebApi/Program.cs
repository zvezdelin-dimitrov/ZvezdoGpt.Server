using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using OpenAI;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();
builder.Services.AddCors();

builder.Services.AddSingleton(new OpenAIClient("").GetChatClient("gpt-4.1-nano"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

//var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? string.Empty;

app.MapPost("/v1/chat/completions", async (HttpContext context, ChatClient chatClient) =>
{
    //context.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var request = await context.Request.ReadFromJsonAsync<ChatCompletionRequest>();
    if (request?.Stream != true)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { Error = "Only stream requests are supported." });
        return;
    }

    var messages = request.Messages
        .Where(dto => dto.Role is "user" or "assistant")
        .Where(dto => dto.Content.ValueKind is JsonValueKind.String or JsonValueKind.Array)
        .Select<ChatMessageDto, ChatMessage>(dto =>
        {
            string content;
            if (dto.Content.ValueKind is JsonValueKind.String)
            {
                content = dto.Content.ToString();
            }
            else
            {
                var parts = JsonSerializer.Deserialize<ChatMessageContentPart[]>(dto.Content.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                content = string.Join(" ", parts.Where(p => p.Type == "text").Select(p => p.Text));
            }

            return dto.Role == "user" ? new UserChatMessage(content) : new AssistantChatMessage(content);
        })
        .ToList();

    context.Response.ContentType = "text/event-stream";

    await foreach (var completionUpdate in chatClient.CompleteChatStreamingAsync(messages))
    {
        var responseContent = completionUpdate.ContentUpdate
            .Where(x => x.Kind is ChatMessageContentPartKind.Text)
            .Select(x => x.Text)
            .Aggregate(new StringBuilder(), (sb, s) => sb.Append(s), sb => sb.ToString());

        if (!string.IsNullOrEmpty(responseContent))
        {
            var payload = new
            {
                id = Guid.NewGuid().ToString(),
                @object = "chat.completion.chunk",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                choices = new[] { new { delta = new { content = responseContent } } }
            };

            var json = JsonSerializer.Serialize(payload);
            await context.Response.WriteAsync($"data: {json}\n\n");
            await context.Response.Body.FlushAsync();
        }
    }

    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();
});
//.RequireAuthorization();

app.Run();

internal class ChatCompletionRequest
{
    public string Model { get; set; }
    public List<ChatMessageDto> Messages { get; set; }
    public bool Stream { get; set; }
    public double? Temperature { get; set; }
    public double? Top_P { get; set; }
    public int? MaxTokens { get; set; }
}

internal class ChatMessageDto
{
    public string Role { get; set; }
    public JsonElement Content { get; set; }
}

internal class ChatMessageContentPart
{
    public string Type { get; set; }
    public string Text { get; set; }
}
