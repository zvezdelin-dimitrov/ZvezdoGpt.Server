internal class ModelsProvider
{
    public ModelsProvider(IConfiguration configuration)
    {
        SupportedModels = configuration["SupportedModels"].Split('|').ToHashSet();
        SupportedModelsResponse = new { data = SupportedModels.Select(model => new { id = model }).ToList() };
    }

    public ISet<string> SupportedModels { get; }

    public object SupportedModelsResponse { get; }
}
