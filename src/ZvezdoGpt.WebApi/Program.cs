using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();
builder.Services.AddCors();

builder.Services.AddSingleton<CosmosDbService>();

builder.Services.AddSingleton<Func<string, string, ChatCompletionService>>(
    (apiKey, model) => new ChatCompletionService(new OpenAIClient(apiKey).GetChatClient(model)));

builder.Services.AddSingleton<Func<string, EmbeddingService>>(
    (apiKey) => new EmbeddingService(new OpenAIClient(apiKey).GetEmbeddingClient(builder.Configuration["EmbeddingModel"])));

builder.Services.AddTransient<ChatCompletionRequestHandler>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapPost("/v1/chat/completions", (ChatCompletionRequestHandler handler) => handler.Handle());//.RequireAuthorization();

app.Run();
