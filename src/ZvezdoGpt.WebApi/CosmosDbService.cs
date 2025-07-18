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

    public async Task SaveApiKey(string username, string apiKey)
    {
        var id = username.ToLowerInvariant();
        try
        {
            await userDataContainer.PatchItemAsync<UserDataResponse>(id, new PartitionKey(id), [PatchOperation.Set("/apiKey", apiKey)]);
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            await userDataContainer.UpsertItemAsync(new { id, apiKey });
        }
    }

    public async Task<string> GetApiKey(string username)
    {
        var id = username.ToLowerInvariant();
        try
        {
            var response = await userDataContainer.ReadItemAsync<UserDataResponse>(id, new PartitionKey(id));
            return response.Resource.ApiKey;
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task SavePreferredModel(string username, string preferredModel)
    {
        var id = username.ToLowerInvariant();
        try
        {
            await userDataContainer.PatchItemAsync<UserDataResponse>(id, new PartitionKey(id), [PatchOperation.Set("/preferredModel", preferredModel)]);
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            await userDataContainer.UpsertItemAsync(new { id, preferredModel });
        }
    }

    public async Task<string> GetPreferredModel(string username)
    {
        var id = username.ToLowerInvariant();
        try
        {
            var response = await userDataContainer.ReadItemAsync<UserDataResponse>(id, new PartitionKey(id));
            return response.Resource.PreferredModel;
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

        public string PreferredModel { get; set; }
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
