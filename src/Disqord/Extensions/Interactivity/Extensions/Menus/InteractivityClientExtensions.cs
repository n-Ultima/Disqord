﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Disqord.Extensions.Interactivity.Menus
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class InteractivityMenuClientExtensions
    {
        public static Task StartMenuAsync(this DiscordClientBase client, Snowflake channelId, MenuBase menu, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            var extension = client.GetInteractivity();
            return extension.StartMenuAsync(channelId, menu, timeout, cancellationToken);
        }

        public static Task RunMenuAsync(this DiscordClientBase client, Snowflake channelId, MenuBase menu, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            var extension = client.GetInteractivity();
            return extension.RunMenuAsync(channelId, menu, timeout, cancellationToken);
        }
    }
}
