internal class ModelsProvider(IConfiguration configuration)
{
    private readonly string[] supportedModels = configuration["SupportedModels"].Split('|');

    public object GetModels() => new { data = supportedModels.Select(model => new { id = model }) };
}
