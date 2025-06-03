using OpenAI.Chat;

internal class ChatCompletionService(ChatClient chatClient)
{
    public async IAsyncEnumerable<string> Complete(IEnumerable<ChatMessage> messages)
    {
        await foreach (var completionUpdate in chatClient.CompleteChatStreamingAsync(messages))
        {
            var response = string.Concat(completionUpdate.ContentUpdate.Where(x => x.Kind is ChatMessageContentPartKind.Text).Select(x => x.Text));

            if (!string.IsNullOrEmpty(response))
            {
                yield return response;
            }
        }
    }
}
