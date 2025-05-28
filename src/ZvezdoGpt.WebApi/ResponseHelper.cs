using System.Text;
using System.Text.Json;
using ZvezdoGpt.WebApi.Dtos;

internal static class ResponseHelper
{
    public static async Task ProcessChatResponse(string chatResponse, HttpResponse httpResponse, StringBuilder aggregateResponse = null)
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

    public static async Task<ChatCompletionRequest> Validate(HttpContext context)
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
