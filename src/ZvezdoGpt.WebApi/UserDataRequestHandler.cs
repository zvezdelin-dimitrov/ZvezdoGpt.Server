internal class UserDataRequestHandler(IHttpContextAccessor contextAccessor, CosmosDbService cosmosDbService)
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
}
