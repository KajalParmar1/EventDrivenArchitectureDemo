using EdaDemo.Messaging;
using Microsoft.Extensions.Logging;

namespace EdaDemo.Contracts.Tests;

public class RetryExecutorTests
{
    [Fact]
    public async Task Succeeds_on_the_first_attempt()
    {
        var executor = new ExponentialBackoffRetryExecutor((_, _) => Task.CompletedTask);
        var logger = new CollectingLogger<RetryExecutorTests>();
        var attempts = 0;

        await executor.ExecuteAsync(
            _ =>
            {
                attempts++;
                return Task.CompletedTask;
            },
            retryCount: 3,
            retryDelaySeconds: 2,
            logger);

        Assert.Equal(1, attempts);
        Assert.DoesNotContain(logger.Messages, m => m.Contains("Retry"));
    }

    [Fact]
    public async Task Retries_until_success_and_logs_each_attempt()
    {
        var executor = new ExponentialBackoffRetryExecutor((_, _) => Task.CompletedTask);
        var logger = new CollectingLogger<RetryExecutorTests>();
        var attempts = 0;

        await executor.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.CompletedTask;
            },
            retryCount: 3,
            retryDelaySeconds: 2,
            logger);

        Assert.Equal(3, attempts);
        Assert.Contains(logger.Messages, m => m.Contains("Processing failed"));
        Assert.Contains(logger.Messages, m => m.Contains("Retry 1/3"));
        Assert.Contains(logger.Messages, m => m.Contains("Retry 2/3"));
        Assert.DoesNotContain(logger.Messages, m => m.Contains("Moving message to DLQ"));
    }

    [Fact]
    public async Task Moves_to_dlq_after_retries_are_exhausted()
    {
        var executor = new ExponentialBackoffRetryExecutor((_, _) => Task.CompletedTask);
        var logger = new CollectingLogger<RetryExecutorTests>();
        var attempts = 0;

        var ex = await Assert.ThrowsAsync<RetryExhaustedException>(() =>
            executor.ExecuteAsync(
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("always fails");
                },
                retryCount: 3,
                retryDelaySeconds: 2,
                logger));

        Assert.Equal(4, attempts);
        Assert.Contains(logger.Messages, m => m.Contains("Retry 1/3"));
        Assert.Contains(logger.Messages, m => m.Contains("Retry 2/3"));
        Assert.Contains(logger.Messages, m => m.Contains("Retry 3/3"));
        Assert.Contains(logger.Messages, m => m.Contains("Moving message to DLQ"));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
