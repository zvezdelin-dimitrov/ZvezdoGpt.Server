using Microsoft.Azure.Cosmos;
using System.Net;

internal class CosmosDbService
{
    private readonly Container questionsContainer;
    private readonly Container userDataContainer;
    private readonly string embeddingModel;
    private readonly double vectorDistanceThreshold;

    public CosmosDbService(IConfiguration configuration)
    {
        var config = configuration.GetSection("CosmosDb").Get<CosmosDbConfig>();
        var dbClient = new CosmosClient(config.Account, config.Key);
        var db = dbClient.GetDatabase(config.DatabaseName);
        questionsContainer = db.GetContainer(config.QuestionsContainerName);
        userDataContainer = db.GetContainer(config.UserDataContainerName);

        embeddingModel = configuration["EmbeddingModel"];
        vectorDistanceThreshold = configuration.GetValue<double>("VectorDistanceThreshold");
    }

    public Task AddQuestion(string question, string answer, float[] vector, string answerGenerationModel) 
        => questionsContainer.CreateItemAsync(new { question, answer, vector, answerGenerationModel, embeddingModel, id = Guid.NewGuid().ToString() });

    public async Task<string> GetAnswer(float[] vector)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 c.answer " +
            "FROM c WHERE c.embeddingModel = @embeddingModel " +
            "AND VectorDistance(c.vector, @embedding) >= @threshold " +
            "ORDER BY VectorDistance(c.vector, @embedding)")
            .WithParameter("@embedding", vector)
            .WithParameter("@embeddingModel", embeddingModel)
            .WithParameter("@threshold", vectorDistanceThreshold);

        using var feed = questionsContainer.GetItemQueryIterator<AnswerResponse>(query);
        var answer = (await feed.ReadNextAsync()).SingleOrDefault();
        return answer?.Answer;
    }

    public Task SaveApiKey(string username, string apiKey)
        => userDataContainer.UpsertItemAsync(new { id = username.ToLowerInvariant(), apiKey });

    public async Task<string> GetApiKey(string username)
    {
        try
        {
            var id = username.ToLowerInvariant();
            var response = await userDataContainer.ReadItemAsync<UserDataResponse>(id, new PartitionKey(id));
            return response.Resource.ApiKey;
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private class AnswerResponse
    {
        public string Answer { get; set; }
    }

    private class UserDataResponse
    {
        public string ApiKey { get; set; }
    }

    private class CosmosDbConfig
    {
        public string Account { get; set; }

        public string Key { get; set; }

        public string DatabaseName { get; set; }

        public string QuestionsContainerName { get; set; }

        public string UserDataContainerName { get; set; }
    }
}
