﻿using System.Threading.Tasks;
using Disqord.Gateway.Api;
using Disqord.Models;

namespace Disqord.Gateway.Default.Dispatcher
{
    public class ChannelDeleteHandler : Handler<ChannelJsonModel, ChannelDeletedEventArgs>
    {
        public override async Task<ChannelDeletedEventArgs> HandleDispatchAsync(IGatewayApiClient shard, ChannelJsonModel model)
        {
            if (!model.GuildId.HasValue)
                return null;

            IGuildChannel channel;
            if (CacheProvider.TryGetChannels(model.GuildId.Value, out var cache) && cache.TryRemove(model.Id, out var cachedChannel))
            {
                channel = cachedChannel;
            }
            else
            {
                channel = TransientGuildChannel.Create(Client, model);
            }

            //  Pass removed messages to e?
            CacheProvider.TryRemoveCache<CachedUserMessage>(model.Id, out _);

            return new ChannelDeletedEventArgs(channel);
        }
    }
}
