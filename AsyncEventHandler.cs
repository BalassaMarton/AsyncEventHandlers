namespace AsyncEventHandlers;

public delegate ValueTask AsyncEventHandler<in TEventArgs>(object sender, TEventArgs args);

[Flags]
public enum AsyncEventInvocationOptions
{
    InvokeInParallel = 0,
    InvokeInOrder = 1,
    FailOnFirstException = 2,
}

public static class AsyncEventHandler
{
    public static async ValueTask InvokeAsync<T>(
        this AsyncEventHandler<T>? eventHandler,
        object sender,
        T args,
        AsyncEventInvocationOptions options = default)
    {
        if (eventHandler == null) return;

        var invocationList = eventHandler.GetInvocationList();

        if (invocationList.Length == 1)
        {
            await ((AsyncEventHandler<T>)invocationList[0])(sender, args);
            return;
        }

        if ((options & AsyncEventInvocationOptions.InvokeInOrder) != AsyncEventInvocationOptions.InvokeInOrder)
        {
            var t = Task.WhenAll(
                invocationList.Select(
                    i => ((AsyncEventHandler<T>)i)(sender, args).AsTask()));

            try
            {
                await t;
            }
            catch
            {
                if (t.Exception!.InnerExceptions.Count == 1)
                    throw t.Exception.InnerExceptions[0];
                else 
                    throw t.Exception!;
            }

            return;
        }

        List<Exception>? exceptions = null;

        foreach (var handler in invocationList)
        {
            try
            {
                await ((AsyncEventHandler<T>)handler)(sender, args);
            }
            catch (Exception e)
            {
                if ((options & AsyncEventInvocationOptions.FailOnFirstException) ==
                    AsyncEventInvocationOptions.FailOnFirstException)
                {
                    throw;
                }

                (exceptions ??= new()).Add(e);
            }
        }

        if (exceptions != null)
        {
            throw new AggregateException(
                "Exceptions were thrown while invoking an async event handler. See the InnerExceptions property for details.",
                exceptions);
        }
    }
}
