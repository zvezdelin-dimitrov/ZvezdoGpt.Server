using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();
builder.Services.AddCors();

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    options.TokenValidationParameters.NameClaimType = "preferred_username");

builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddSingleton<ModelsProvider>();
builder.Services.AddTransient<UserDataRequestHandler>();

builder.Services.AddSingleton<Func<string, string, ChatCompletionService>>(
    (apiKey, model) => new ChatCompletionService(new OpenAIClient(apiKey).GetChatClient(model)));

builder.Services.AddSingleton<Func<string, EmbeddingService>>(
    (apiKey) => new EmbeddingService(new OpenAIClient(apiKey).GetEmbeddingClient(builder.Configuration["EmbeddingModel"])));

builder.Services.AddTransient<Func<bool, ChatCompletionRequestHandler>>(serviceProvider =>
    openAiCompatible => ActivatorUtilities.CreateInstance<ChatCompletionRequestHandler>(serviceProvider, openAiCompatible));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapPost("/v9/user/apikey", (UserDataRequestHandler handler) => handler.SaveApiKey()).RequireAuthorization();

app.MapGet("/v9/models", (ModelsProvider provider) => provider.SupportedModelsResponse);
app.MapGet("/v1/models", (ModelsProvider provider) => provider.SupportedModelsResponse);

app.MapPost("/v9/chat/completions", (Func<bool, ChatCompletionRequestHandler> handler) => handler(false).Handle());
app.MapPost("/v1/chat/completions", (Func<bool, ChatCompletionRequestHandler> handler) => handler(true).Handle());

app.Run();
