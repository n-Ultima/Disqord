﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Logging;
using Disqord.Utilities.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Disqord.Hosting
{
    /// <summary>
    ///     Represents an <see cref="IHostedService"/> base class for Discord client services.
    /// </summary>
    /// <remarks>
    ///     If the implementation overrides <see cref="ExecuteAsync(CancellationToken)"/>
    ///     the service will additionally act as an improved version of <see cref="BackgroundService"/>.
    /// </remarks>
    public abstract class DiscordClientService : IHostedService, IDisposable, ILogging
    {
        public ILogger Logger { get; }

        public virtual DiscordClientBase Client { get; }

        /// <summary>
        ///     Gets the <see cref="Task"/> that represents the long-running work from <see cref="ExecuteAsync(CancellationToken)"/>.
        /// </summary>
        /// <remarks>
        ///     Returns <see langword="null"/> if the background operation has not started or <see cref="ExecuteAsync(CancellationToken)"/> is not overridden.
        /// </remarks>
        public virtual Task ExecuteTask => _executeTask;

        private Task _executeTask;
        private Cts _cts;

        protected DiscordClientService(
            ILogger logger,
            DiscordClientBase client)
        {
            Logger = logger;
            Client = client;
        }

        /// <summary>
        ///     This method is called when the <see cref="IHostedService"/> starts if it has been overridden in the implementing type.
        ///     The implementation should return a <see cref="Task"/> representing the long-running work.
        /// </summary>
        /// <param name="stoppingToken"> Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called. </param>
        /// <returns>
        ///     A <see cref="Task"/> that represents the long-running work.
        /// </returns>
        protected virtual Task ExecuteAsync(CancellationToken stoppingToken)
            => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            // I assume the user correctly overrides the method and doesn't hide it.
            var method = GetType().GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method.DeclaringType == typeof(DiscordClientService))
                return Task.CompletedTask;

            _cts = Cts.Linked(cancellationToken);
            _executeTask = Task.Run(async () =>
            {
                try
                {
                    await ExecuteAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An exception occurred while executing {0}.", GetType().Name);
                    throw;
                }
            }, cancellationToken);

            if (_executeTask.IsCompleted)
                return _executeTask;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executeTask == null)
                return;

            try
            {
                _cts.Cancel();
            }
            finally
            {
                var delayTask = Task.Delay(Timeout.Infinite, cancellationToken);
                await Task.WhenAny(_executeTask, delayTask).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
