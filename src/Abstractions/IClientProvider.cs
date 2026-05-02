using Microsoft.Extensions.AI;

namespace AI.Agents.Abstractions;

public interface IClientProvider
{
    bool CanCreateChatClient(IServiceProvider serviceProvider);

    IChatClient CreateChatClient(IServiceProvider serviceProvider);
}