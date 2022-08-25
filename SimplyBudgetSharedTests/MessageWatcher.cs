using CommunityToolkit.Mvvm.Messaging;

namespace SimplyBudgetSharedTests;

public class MessageWatcher<TMessage> : IRecipient<TMessage>
    where TMessage : class
{
    private readonly List<TMessage> _Messages = new();
    public IReadOnlyList<TMessage> Messages => _Messages.AsReadOnly();

    public void Receive(TMessage message)
    {
        lock (_Messages)
        {
            _Messages.Add(message);
        }
    }
}
