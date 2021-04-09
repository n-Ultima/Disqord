﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Disqord.Rest;

namespace Disqord.Bot
{
    public class DiscordResponseCommandResult : DiscordCommandResult
    {
        public virtual LocalMessage Message { get; }

        public DiscordResponseCommandResult(DiscordCommandContext context, LocalMessage message)
            : base(context)
        {
            Message = message;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new virtual TaskAwaiter<IUserMessage> GetAwaiter()
            => ExecuteAsync().GetAwaiter();

        public override Task<IUserMessage> ExecuteAsync()
            => Context.Bot.SendMessageAsync(Context.ChannelId, Message);
    }
}
