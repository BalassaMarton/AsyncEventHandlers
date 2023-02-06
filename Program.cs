namespace AsyncEventHandlers
{
#nullable enable
    using System;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main()
        {
            await InvokeInParallel();
            Console.WriteLine();
            await InvokeInOrder();
            Console.WriteLine();
            await InvokeWithExceptions();
            Console.WriteLine();
            await InvokeWithExceptionsFailFast();
            Console.WriteLine();
        }

        private static async Task InvokeInParallel()
        {
            var source = new AsyncEventSource<string>();

            source.Event += async (sender, args) => {
                Console.WriteLine($"First handler ({args}) before delay");
                await Task.Delay(100);
                Console.WriteLine("First handler after delay");
            };

            source.Event += async (sender, args) => {
                Console.WriteLine($"Second handler ({args}) before delay");
                await Task.Delay(200);
                Console.WriteLine("Second handler after delay");
            };

            Console.WriteLine("Invoking async event handlers in parallel");

            await source.RaiseEventAsync("hello", default);

            Console.WriteLine("Done invoking async event handlers");
        }

        private static async Task InvokeInOrder()
        {
            var source = new AsyncEventSource<string>();

            source.Event += async (sender, args) => {
                Console.WriteLine($"First handler ({args}) before delay");
                await Task.Delay(100);
                Console.WriteLine("First handler after delay");
            };

            source.Event += async (sender, args) => {
                Console.WriteLine($"Second handler ({args}) before delay");
                await Task.Delay(200);
                Console.WriteLine("Second handler after delay");
            };

            Console.WriteLine("Invoking async event handlers in order");

            await source.RaiseEventAsync("hello", AsyncEventInvocationOptions.InvokeInOrder);

            Console.WriteLine("Done invoking async event handlers");
        }

        private static async Task InvokeWithExceptions()
        {
            var source = new AsyncEventSource<string>();

            source.Event += async (sender, args) => {
                Console.WriteLine($"First handler ({args}) before delay");
                await Task.Delay(100);
                Console.WriteLine("First handler after delay");
                throw new InvalidOperationException("First exception");
            };

            source.Event += async (sender, args) => {
                Console.WriteLine($"Second handler ({args}) before delay");
                await Task.Delay(200);
                Console.WriteLine("Second handler after delay");
                throw new InvalidOperationException("Second exception");
            };

            Console.WriteLine("Invoking async event handlers with exceptions");

            try
            {
                await source.RaiseEventAsync("hello", default);
                Console.WriteLine("Done invoking async event handlers");
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

        private static async Task InvokeWithExceptionsFailFast()
        {
            var source = new AsyncEventSource<string>();

            source.Event += async (sender, args) => {
                Console.WriteLine($"First handler ({args}) before delay");
                await Task.Delay(100);
                Console.WriteLine("First handler after delay");
                throw new InvalidOperationException("First exception");
            };

            source.Event += async (sender, args) => {
                Console.WriteLine($"Second handler ({args}) before delay");
                await Task.Delay(200);
                Console.WriteLine("Second handler after delay");
                throw new InvalidOperationException("Second exception");
            };

            Console.WriteLine("Invoking async event handlers in order, failing on first exception");

            try
            {
                await source.RaiseEventAsync("hello", AsyncEventInvocationOptions.InvokeInOrder | AsyncEventInvocationOptions.FailOnFirstException);
                Console.WriteLine("Done invoking async event handlers");
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

        private static void LogException(Exception exception)
        {
            Console.WriteLine($"Exception {exception.GetType().FullName} with message '{exception.Message}'");
            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    Console.WriteLine(
                        $"  Inner exception {innerException.GetType().FullName} with message '{innerException.Message}'");
                }
            }
            else if (exception.InnerException != null)
            {
                Console.WriteLine(
                    $"  Inner exception {exception.InnerException.GetType().FullName} with message '{exception.InnerException.Message}'");
            }
        }
    }

    public class AsyncEventSource<T>
    {

        public event AsyncEventHandler<T>? Event;

        public async Task RaiseEventAsync(T args, AsyncEventInvocationOptions options)
        {
            await Event.InvokeAsync(this, args, options);
        }
    }
}