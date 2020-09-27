using Microsoft.Toolkit.Mvvm.Messaging;
using System.Collections.Generic;

namespace SimplyBudgetSharedTests
{
    public class MessageWatcher<TMessage> : IRecipient<TMessage>
        where TMessage : class
    {
        private readonly List<TMessage> _Messages = new List<TMessage>();
        public IReadOnlyList<TMessage> Messages => _Messages.AsReadOnly();

        public void Receive(TMessage message)
        {
            lock (_Messages)
            {
                _Messages.Add(message);
            }
        }
    }
}
