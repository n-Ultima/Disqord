﻿using System;
using System.Threading;
using Disqord.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Disqord.Bot
{
    public static class DiscordBotServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscordBot(this IServiceCollection services, Action<DiscordBotConfiguration> configure = null)
        {
            services.AddDiscordBot<DiscordBot>();

            var options = services.AddOptions<DiscordBotConfiguration>();
            if (configure != null)
                options.Configure(configure);

            return services;
        }

        public static IServiceCollection AddDiscordBot<TDiscordBot>(this IServiceCollection services)
            where TDiscordBot : DiscordBot
        {
            services.AddDiscordClient();

            if (services.TryAddSingleton<TDiscordBot>())
            {
                var callCount = 0;

                TDiscordBot GetBotService(IServiceProvider services)
                {
                    if (Interlocked.Increment(ref callCount) > 1)
                        throw new InvalidOperationException($"Disqord detected a circular dependency for the bot client of type '{typeof(TDiscordBot)}'. "
                            + "This means that most likely your prefix provider or another service depends on the bot client and vice versa.");

                    var service = services.GetRequiredService<TDiscordBot>();
                    Interlocked.Decrement(ref callCount);
                    return service;
                }

                if (typeof(TDiscordBot) != typeof(DiscordBot))
                    services.TryAddSingleton<DiscordBot>(GetBotService);

                services.TryAddSingleton<DiscordBotBase>(GetBotService);
                services.Replace(ServiceDescriptor.Singleton<DiscordClientBase>(GetBotService));
            }

            services.AddPrefixProvider();
            services.AddCommandQueue();
            services.AddCommands();
            services.AddCommandContextAccessor();
            return services;
        }

        public static IServiceCollection AddPrefixProvider<TPrefixProvider>(this IServiceCollection services)
            where TPrefixProvider : class, IPrefixProvider
        {
            services.TryAddSingleton<IPrefixProvider, TPrefixProvider>();
            return services;
        }

        public static IServiceCollection AddPrefixProvider(this IServiceCollection services, Action<DefaultPrefixProviderConfiguration> configure = null)
        {
            if (services.TryAddSingleton<IPrefixProvider, DefaultPrefixProvider>())
            {
                var options = services.AddOptions<DefaultPrefixProviderConfiguration>();
                if (configure != null)
                    options.Configure(configure);
            }

            return services;
        }

        public static IServiceCollection AddCommandQueue(this IServiceCollection services, Action<DefaultCommandQueueConfiguration> configure = null)
        {
            if (services.TryAddSingleton<ICommandQueue, DefaultCommandQueue>())
            {
                var options = services.AddOptions<DefaultCommandQueueConfiguration>();
                if (configure != null)
                    options.Configure(configure);
            }

            return services;
        }

        public static IServiceCollection AddCommands(this IServiceCollection services, Action<CommandServiceConfiguration> configure = null)
        {
            if (services.TryAddSingleton(services =>
            {
                var options = services.GetRequiredService<IOptions<CommandServiceConfiguration>>();
                return new CommandService(options.Value);
            }))
            {
                services.AddOptions<CommandServiceConfiguration>();
                if (configure != null)
                    services.Configure(configure);
            }

            return services;
        }

        public static IServiceCollection AddCommandContextAccessor(this IServiceCollection services)
        {
            services.TryAddScoped<ICommandContextAccessor, DefaultCommandContextAccessor>();
            return services;
        }
    }
}
