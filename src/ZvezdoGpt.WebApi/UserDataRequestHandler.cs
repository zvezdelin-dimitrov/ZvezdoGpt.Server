internal class UserDataRequestHandler(IHttpContextAccessor contextAccessor, CosmosDbService cosmosDbService, ModelsProvider modelsProvider)
{
    public async Task<IResult> SaveApiKey()
    {
        var apiKey = contextAccessor.HttpContext.Request.Headers["X-API-KEY"].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Results.BadRequest();
        }

        await cosmosDbService.SaveApiKey(contextAccessor.HttpContext.User.Identity.Name, apiKey);

        return Results.Ok();
    }

    public async Task<IResult> SavePreferredModel()
    {
        var model = await contextAccessor.HttpContext.Request.ReadFromJsonAsync<string>();
        if (string.IsNullOrWhiteSpace(model) || !modelsProvider.SupportedModels.Contains(model)) 
        {
            return Results.BadRequest();
        }

        await cosmosDbService.SavePreferredModel(contextAccessor.HttpContext.User.Identity.Name, model);

        return Results.Ok();
    }

    public Task<string> GetPreferredModel()
        => cosmosDbService.GetPreferredModel(contextAccessor.HttpContext.User.Identity.Name);
}
