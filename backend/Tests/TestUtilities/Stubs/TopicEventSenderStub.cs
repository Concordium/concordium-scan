using System.Threading;
using HotChocolate.Subscriptions;

namespace Tests.TestUtilities.Stubs;

public class TopicEventSenderStub : ITopicEventSender
{
    public ValueTask SendAsync<TTopic, TMessage>(TTopic topic, TMessage message,
        CancellationToken cancellationToken = new CancellationToken()) where TTopic : notnull
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync<TTopic>(TTopic topic) where TTopic : notnull
    {
        return ValueTask.CompletedTask;
    }
}