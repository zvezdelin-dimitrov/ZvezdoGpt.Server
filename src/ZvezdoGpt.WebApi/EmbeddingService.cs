using OpenAI.Embeddings;

internal class EmbeddingService(EmbeddingClient embeddingClient)
{
    public async Task<float[]> GetVector(string question)
    {
        var embedding = await embeddingClient.GenerateEmbeddingAsync(question);
        return embedding.Value.ToFloats().ToArray();
    }
}
