﻿using System;
using Disqord.Serialization.Json;

namespace Disqord.Models
{
    public class EmojiJsonModel : JsonModel, IEquatable<IEmoji>
    {
        [JsonProperty("id")]
        public Snowflake? Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("roles")]
        public Optional<Snowflake[]> Roles;

        [JsonProperty("user")]
        public Optional<UserJsonModel> User;

        [JsonProperty("require_colons")]
        public Optional<bool> RequireColons;

        [JsonProperty("managed")]
        public Optional<bool> Managed;

        [JsonProperty("animated")]
        public Optional<bool> Animated;

        [JsonProperty("available")]
        public Optional<bool> Available;

        public bool Equals(IEmoji other)
        {
            if (other == null)
                return false;

            var id = Id;
            if (id != null)
            {
                if (other is not ICustomEmoji customEmoji)
                    return false;

                return id.Value == customEmoji.Id;
            }

            return Name == other.Name;
        }
    }
}
