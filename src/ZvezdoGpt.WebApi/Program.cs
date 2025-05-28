using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenAI;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();
builder.Services.AddCors();

builder.Services.AddSingleton<CosmosDbService>();

builder.Services.AddSingleton<Func<string, string, ChatCompletionService>>(
    (apiKey, model) => new ChatCompletionService(new OpenAIClient(apiKey).GetChatClient(model)));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapPost("/v1/chat/completions", async (HttpContext context, Func<string, string, ChatCompletionService> chatServiceFactory, CosmosDbService cosmosDbService) =>
{
    var request = await ResponseHelper.Validate(context);
    if (request is null)
    {
        return;
    }

    context.Response.ContentType = "text/event-stream";

    if (request.Messages.Count == 1)
    {
        var question = request.Messages[0].Content.ToString();
        var embedding = await new OpenAIClient(request.ApiKey).GetEmbeddingClient("text-embedding-3-small").GenerateEmbeddingAsync(question);
        var vector = embedding.Value.ToFloats().ToArray();
        var cachedAnswer = await cosmosDbService.GetAnswer(vector);
        
        if (cachedAnswer is not null)
        {
            await ResponseHelper.ProcessChatResponse(cachedAnswer, context.Response);
        }
        else
        {
            var aggregatedAnswer = new StringBuilder();

            await foreach (var chatResponse in chatServiceFactory(request.ApiKey, request.Model).Complete(request.Messages))
            {
                await ResponseHelper.ProcessChatResponse(chatResponse, context.Response, aggregatedAnswer);
            }

            await cosmosDbService.AddQuestion(question, aggregatedAnswer.ToString(), vector);
        }
    }
    else
    {
        await foreach (var chatResponse in chatServiceFactory(request.ApiKey, request.Model).Complete(request.Messages))
        {
            await ResponseHelper.ProcessChatResponse(chatResponse, context.Response);
        }
    }

    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();
});
//.RequireAuthorization();

app.Run();
