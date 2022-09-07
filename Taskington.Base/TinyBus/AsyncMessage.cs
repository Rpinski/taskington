using System;
using System.Threading.Tasks;

namespace Taskington.Base.TinyBus;

public abstract record AsyncMessageData<T> where T : AsyncMessageData<T>
{
    private static AsyncMessage<T> messageEndPoint = new();

    public async Task Publish() =>
        await messageEndPoint.Push((T) this);

    public static void Subscribe(Func<T, Task> subscriber) =>
        messageEndPoint.Subscribe(subscriber);

    public static void Subscribe(Func<T, Task> subscriber, Func<T, bool> predicate) =>
        messageEndPoint.Subscribe(subscriber, predicate);

    protected static void Unsubscribe(Func<T, Task> subscriber) =>
        messageEndPoint.Unsubscribe(subscriber);

    protected static void UnsubscribeAll() =>
        messageEndPoint.UnsubscribeAll();
}
