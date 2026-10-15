using Microsoft.Extensions.DependencyInjection;

namespace EdaDemo.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddEdaMessaging<THandler>(this IServiceCollection services)
        where THandler : class, IKafkaMessageHandler
    {
        services.AddSingleton<ExponentialBackoffRetryExecutor>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddSingleton<IDeadLetterPublisher, KafkaDeadLetterPublisher>();
        services.AddSingleton<IKafkaMessageHandler, THandler>();
        services.AddHostedService<ReliableKafkaConsumer>();
        return services;
    }
}
