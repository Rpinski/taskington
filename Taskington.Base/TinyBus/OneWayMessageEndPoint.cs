using System;
using System.Collections.Generic;

namespace Taskington.Base.TinyBus
{
    public abstract class OneWayMessage<F, P> : Subscribable<F, P>
        where F : notnull
        where P : notnull
    {
        protected void Invoke(Action<F> invoker, Predicate<P> predicateInvoker)
        {
            foreach (var subscriberInfo in Subscriptions)
            {
                if ((subscriberInfo.Predicate == null) || predicateInvoker(subscriberInfo.Predicate))
                {
                    invoker(subscriberInfo.Subscriber);
                }
            }
        }
    }
}
