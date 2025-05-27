using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenAI;
using ZvezdoGpt.WebApi.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();
builder.Services.AddCors();

builder.Services.AddSingleton<Func<string, string, ChatCompletionService>>(
    (apiKey, model) => new ChatCompletionService(new OpenAIClient(apiKey).GetChatClient(model)));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

//var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? string.Empty;

app.MapPost("/v1/chat/completions", async (HttpContext context, Func<string, string, ChatCompletionService> chatServiceFactory) =>
{
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
        return;
    }

    var request = await context.Request.ReadFromJsonAsync<ChatCompletionRequest>();
    if (request is null || !request.Stream || string.IsNullOrEmpty(request.Model))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    context.Response.ContentType = "text/event-stream";

    await foreach (var jsonResponse in chatServiceFactory(apiKey, request.Model).Complete(request.Messages))
    {
        await context.Response.WriteAsync($"data: {jsonResponse}\n\n");
        await context.Response.Body.FlushAsync();
    }

    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();
});
//.RequireAuthorization();

app.Run();
