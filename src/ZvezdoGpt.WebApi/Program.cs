using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using OpenAI;
using ZvezdoGpt.WebApi.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();
builder.Services.AddCors();

builder.Services.AddSingleton(new OpenAIClient("").GetChatClient("gpt-4.1-nano"));
builder.Services.AddSingleton<ChatCompletionService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

//var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? string.Empty;

app.MapPost("/v1/chat/completions", async (HttpContext context, ChatCompletionService chatService) =>
{
    //context.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var request = await context.Request.ReadFromJsonAsync<ChatCompletionRequest>();
    if (request?.Stream != true)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { Error = "Only stream requests are supported." });
        return;
    }

    context.Response.ContentType = "text/event-stream";

    await foreach (var jsonResponse in chatService.Complete(request.Messages))
    {
        await context.Response.WriteAsync($"data: {jsonResponse}\n\n");
        await context.Response.Body.FlushAsync();
    }

    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();
});
//.RequireAuthorization();

app.Run();
