using Microsoft.Azure.Cosmos;

internal class CosmosDbService
{
    private readonly Container container;
    private readonly string embeddingModel;

    public CosmosDbService(IConfiguration configuration)
    {
        var config = configuration.GetSection("CosmosDb").Get<CosmosDbConfig>();
        var dbClient = new CosmosClient(config.Account, config.Key);
        var db = dbClient.GetDatabase(config.DatabaseName);
        container = db.GetContainer(config.ContainerName);
        embeddingModel = configuration["EmbeddingModel"];
    }

    public Task AddQuestion(string question, string answer, float[] vector, string answerGenerationModel) 
        => container.CreateItemAsync(new { question, answer, vector, answerGenerationModel, embeddingModel, id = Guid.NewGuid().ToString() });

    public async Task<string> GetAnswer(float[] vector)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 c.answer " +
            "FROM c WHERE c.embeddingModel = @embeddingModel " +
            "AND VectorDistance(c.vector, @embedding) >= @threshold " +
            "ORDER BY VectorDistance(c.vector, @embedding)")
            .WithParameter("@embedding", vector)
            .WithParameter("@embeddingModel", embeddingModel)
            .WithParameter("@threshold", 0.8);

        using var feed = container.GetItemQueryIterator<AnswerResponse>(query);
        var answer = (await feed.ReadNextAsync()).SingleOrDefault();
        return answer?.Answer;
    }

    private class AnswerResponse
    {
        public string Answer { get; set; }
    }

    private class CosmosDbConfig
    {
        public string Account { get; set; }

        public string Key { get; set; }

        public string DatabaseName { get; set; }

        public string ContainerName { get; set; }
    }
}
