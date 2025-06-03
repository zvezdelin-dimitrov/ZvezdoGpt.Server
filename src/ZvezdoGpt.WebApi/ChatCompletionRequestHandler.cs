using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using ZvezdoGpt.WebApi.Dtos;

internal class ChatCompletionRequestHandler(IHttpContextAccessor contextAccessor, IConfiguration configuration, Func<string, string, ChatCompletionService> chatServiceFactory, Func<string, EmbeddingService> embeddingServiceFactory, CosmosDbService cosmosDbService)
{
    private readonly HttpContext context = contextAccessor.HttpContext;
    private readonly HashSet<int> cacheWindowSizes = new(Enumerable.Range(1, configuration.GetValue("ContextWindow", 1)).Where(i => i % 2 == 1));

    public async Task Handle()
    {
        var request = await Validate(context);
        if (request is null)
        {
            return;
        }

        context.Response.ContentType = "text/event-stream";

        var messages = request.Messages.ToChatMessages().ToList();

        if (cacheWindowSizes.Contains(messages.Count))
        {
            var question = string.Join("|", messages.Select(m => $"{(m is UserChatMessage ? "user" : "assistant")}:{string.Join(string.Empty, m.Content.Select(x => x.Text))}"));
            var vector = await embeddingServiceFactory(request.ApiKey).GetVector(question);
            var cachedAnswer = await cosmosDbService.GetAnswer(vector);

            if (cachedAnswer is not null)
            {
                await ProcessChatResponse(cachedAnswer, context.Response);
            }
            else
            {
                var aggregatedAnswer = new StringBuilder();

                await foreach (var chatResponse in chatServiceFactory(request.ApiKey, request.Model).Complete(messages))
                {
                    await ProcessChatResponse(chatResponse, context.Response, aggregatedAnswer);
                }

                await cosmosDbService.AddQuestion(question, aggregatedAnswer.ToString(), vector);
            }
        }
        else
        {
            await foreach (var chatResponse in chatServiceFactory(request.ApiKey, request.Model).Complete(messages))
            {
                await ProcessChatResponse(chatResponse, context.Response);
            }
        }

        await context.Response.WriteAsync("data: [DONE]\n\n");
        await context.Response.Body.FlushAsync();
    }

    private static async Task ProcessChatResponse(string chatResponse, HttpResponse httpResponse, StringBuilder aggregateResponse = null)
    {
        var payload = new
        {
            id = Guid.NewGuid().ToString(),
            @object = "chat.completion.chunk",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            choices = new[] { new { delta = new { content = chatResponse } } }
        };

        await httpResponse.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n");
        await httpResponse.Body.FlushAsync();

        aggregateResponse?.Append(chatResponse);
    }

    private static async Task<ChatCompletionRequest> Validate(HttpContext context)
    {
        //var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"];
        //context.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

        string apiKey = null;
        try
        {
            apiKey = context.Request.Headers.Authorization[0]["Bearer ".Length..].Trim();
        }
        catch
        {
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return null;
        }

        var request = await context.Request.ReadFromJsonAsync<ChatCompletionRequest>();
        if (request is null || !request.Stream || string.IsNullOrEmpty(request.Model))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return null;
        }

        request.ApiKey = apiKey;

        return request;
    }
}
