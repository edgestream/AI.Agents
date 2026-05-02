using System.ClientModel.Primitives;

namespace AI.Microsoft.Client;

internal class CodexHeadersPolicy : PipelinePolicy
{
    private readonly string _accountId;

    public CodexHeadersPolicy(string accountId)
    {
        _accountId = accountId;
    }

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddHeaders(message);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddHeaders(message);
        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    private void AddHeaders(PipelineMessage message)
    {
        message.Request.Headers.Set("chatgpt-account-id", _accountId);
        message.Request.Headers.Set("OpenAI-Beta", "responses=experimental");
    }
}
