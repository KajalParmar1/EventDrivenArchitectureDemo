using Microsoft.Extensions.Logging;

namespace EdaDemo.Messaging;

public sealed class ExponentialBackoffRetryExecutor
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public ExponentialBackoffRetryExecutor(Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _delay = delay ?? ((time, token) => Task.Delay(time, token));
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        int retryCount,
        int retryDelaySeconds,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);
        ArgumentOutOfRangeException.ThrowIfNegative(retryDelaySeconds);

        try
        {
            await action(cancellationToken);
            return;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception firstException)
        {
            logger.LogWarning(
                firstException,
                "Processing failed.");

            Exception lastException = firstException;

            for (var attempt = 1; attempt <= retryCount; attempt++)
            {
                logger.LogWarning("Retry {Attempt}/{RetryCount}", attempt, retryCount);

                var delay = TimeSpan.FromSeconds(retryDelaySeconds * Math.Pow(2, attempt - 1));
                await _delay(delay, cancellationToken);

                try
                {
                    await action(cancellationToken);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception retryException)
                {
                    lastException = retryException;
                    logger.LogWarning(
                        retryException,
                        "Processing failed.");
                }
            }

            logger.LogError(
                lastException,
                "Moving message to DLQ.");

            throw new RetryExhaustedException(
                $"Processing failed after {retryCount} retries.",
                lastException);
        }
    }
}
