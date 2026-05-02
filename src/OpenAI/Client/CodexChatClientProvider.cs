using AI.Agents.Abstractions;
using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // 'OpenAI.Responses.ResponsesClient' is for evaluation purposes only

namespace AI.Agents.OpenAI.Client;

internal sealed class CodexChatClientProvider : IClientProvider
{
    public bool CanCreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetService<IOptions<CodexSettings>>()?.Value;
        return settings is not null && HasValidSettings(settings);
    }

    public IChatClient CreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<IOptions<CodexSettings>>().Value;
        return CreateChatClient(settings);
    }

    internal static IChatClient CreateChatClient(CodexSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateSettings(settings);

        var options = OpenAIChatClientProvider.CreateOpenAIClientOptions(settings);
        var accountId = settings.AccountID!;
        options.AddPolicy(new CodexHeadersPolicy(accountId), PipelinePosition.PerCall);

        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options)
            .GetResponsesClient()
            .AsIChatClient(settings.Model)
            .AsBuilder()
            .ConfigureOptions(options =>
            {
                options.RawRepresentationFactory = _ =>
                {
                    var raw = new CreateResponseOptions
                    {
                        StoredOutputEnabled = false,
                        StreamingEnabled = true
                    };
                    raw.IncludedProperties.Add(IncludedResponseProperty.ReasoningEncryptedContent);
                    return raw;
                };
            })
            .Build();
    }

    internal static bool HasValidSettings(CodexSettings settings)
    {
        try
        {
            ValidateSettings(settings);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    internal static void ValidateSettings(CodexSettings settings)
    {
        OpenAIChatClientProvider.ValidateSettings(settings, "Codex");
        if (string.IsNullOrWhiteSpace(settings.AccountID))
        {
            throw new InvalidOperationException("Codex:AccountID is not configured.");
        }
    }
}
