﻿using Disqord.Serialization.Json;

namespace Disqord.Rest.Api
{
    public class CreateBanJsonRestRequestContent : JsonModelRestRequestContent
    {
        [JsonProperty("delete_message_days")]
        public Optional<int> DeleteMessageDays;

        [JsonProperty("reason")]
        public Optional<string> Reason;
    }
}
