﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Http;
using Disqord.Models;
using Disqord.Rest.Api;
using Qommon;

namespace Disqord.Rest
{
    public static partial class RestClientExtensions
    {
        public static Task CreateInteractionResponseAsync(this IRestClient client,
            Snowflake interactionId, string interactionToken, LocalInteractionResponse response,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.IsNotNull(response);

            response.Validate();
            var messageContent = new CreateInitialInteractionResponseJsonRestRequestContent
            {
                Type = response.Type
            };

            if (messageContent.Type != InteractionResponseType.Pong)
            {
                if (messageContent.Type is not (InteractionResponseType.MessageUpdate or InteractionResponseType.DeferredMessageUpdate))
                {
                    messageContent.Data = new InteractionCallbackDataJsonModel
                    {
                        Tts = Optional.Conditional(response.IsTextToSpeech, true),
                        Content = Optional.FromNullable(response.Content),
                        Embeds = Optional.Conditional(response.Embeds.Count != 0, x => x.Select(x => x.ToModel()).ToArray(), response.Embeds),
                        AllowedMentions = Optional.FromNullable(response.AllowedMentions.ToModel()),
                        Components = Optional.Conditional(response.Components.Count != 0, x => x.Select(x => x.ToModel()).ToArray(), response.Components),
                        Flags = Optional.Conditional(response.Flags != InteractionResponseFlag.None, response.Flags)
                    };
                }
                else
                {
                    // TODO: make properties properly optional via different LocalInteractionResponse types?
                    messageContent.Data = new InteractionCallbackDataJsonModel
                    {
                        Content = response.Content,
                        Embeds = response.Embeds.Select(x => x.ToModel()).ToArray(),
                        AllowedMentions = response.AllowedMentions?.ToModel(),
                        Components = response.Components.Select(x => x.ToModel()).ToArray(),
                        Flags = response.Flags
                    };
                }
            }

            Task task;
            if (response.Attachments.Count != 0)
            {
                // If there is an attachment, we must send it via multipart HTTP content.
                // Our `messageContent` will be serialized into a "payload_json" form data field.
                var content = new MultipartJsonPayloadRestRequestContent<CreateInitialInteractionResponseJsonRestRequestContent>(messageContent, response.Attachments);
                task = client.ApiClient.CreateInitialInteractionResponseAsync(interactionId, interactionToken, content, options, cancellationToken);
            }
            else
            {
                task = client.ApiClient.CreateInitialInteractionResponseAsync(interactionId, interactionToken, messageContent, options, cancellationToken);
            }

            return task;
        }

        public static async Task<IUserMessage> FetchInteractionResponseAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            var model = await client.ApiClient.FetchInitialInteractionResponseAsync(applicationId, interactionToken, options, cancellationToken).ConfigureAwait(false);
            return new TransientUserMessage(client, model);
        }

        public static Task<IUserMessage> ModifyInteractionResponseAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken, Action<ModifyWebhookMessageActionProperties> action,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.InternalModifyInteractionResponseAsync(applicationId, interactionToken, null, action, options, cancellationToken);
        }

        public static Task DeleteInteractionResponseAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.ApiClient.DeleteInitialInteractionResponseAsync(applicationId, interactionToken, options, cancellationToken);
        }

        public static async Task<IUserMessage> CreateInteractionFollowupAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken, LocalInteractionFollowup followup,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.IsNotNull(followup);

            followup.Validate();
            var messageContent = new CreateFollowupMessageJsonRestRequestContent
            {
                Content = Optional.FromNullable(followup.Content),
                Tts = Optional.Conditional(followup.IsTextToSpeech, true),
                Embeds = Optional.Conditional(followup.Embeds.Count != 0, x => x.Select(x => x.ToModel()).ToArray(), followup.Embeds),
                AllowedMentions = Optional.FromNullable(followup.AllowedMentions.ToModel()),
                Components = Optional.Conditional(followup.Components.Count != 0, x => x.Select(x => x.ToModel()).ToArray(), followup.Components),
                Flags = followup.Flags
            };

            Task<MessageJsonModel> task;
            if (followup.Attachments.Count != 0)
            {
                // If there are attachments, we must send them via multipart HTTP content.
                // Our `messageContent` will be serialized into a "payload_json" form data field.
                var content = new MultipartJsonPayloadRestRequestContent<CreateFollowupMessageJsonRestRequestContent>(messageContent, followup.Attachments);
                task = client.ApiClient.CreateFollowupInteractionResponseAsync(applicationId, interactionToken, content, options, cancellationToken);
            }
            else
            {
                task = client.ApiClient.CreateFollowupInteractionResponseAsync(applicationId, interactionToken, messageContent, options, cancellationToken);
            }

            var model = await task.ConfigureAwait(false);
            return new TransientUserMessage(client, model);
        }

        public static async Task<IUserMessage> FetchInteractionFollowupAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken, Snowflake messageId,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await client.ApiClient.FetchFollowupInteractionResponseAsync(applicationId, interactionToken, messageId, options, cancellationToken).ConfigureAwait(false);
                return new TransientUserMessage(client, model);
            }
            catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.NotFound && ex.ErrorModel.Code == RestApiErrorCode.UnknownMessage)
            {
                return null;
            }
        }

        public static Task<IUserMessage> ModifyInteractionFollowupAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken, Snowflake messageId, Action<ModifyWebhookMessageActionProperties> action,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.InternalModifyInteractionResponseAsync(applicationId, interactionToken, messageId, action, options, cancellationToken);
        }

        private static async Task<IUserMessage> InternalModifyInteractionResponseAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken, Snowflake? messageId, Action<ModifyWebhookMessageActionProperties> action,
            IRestRequestOptions options, CancellationToken cancellationToken)
        {
            var properties = new ModifyWebhookMessageActionProperties();
            action.Invoke(properties);
            var messageContent = new ModifyWebhookMessageJsonRestRequestContent
            {
                Content = properties.Content,
                Embeds = Optional.Convert(properties.Embeds, x => x.Select(x =>
                {
                    x.Validate();
                    return x.ToModel();
                }).ToArray()),
                AllowedMentions = Optional.Convert(properties.AllowedMentions, x => x.ToModel()),
                Attachments = Optional.Convert(properties.AttachmentIds, x => x.Select(x => new AttachmentJsonModel
                {
                    Id = x
                }).ToArray()),
                Components = Optional.Convert(properties.Components, x => x.Select(x =>
                {
                    x.Validate();
                    return x.ToModel();
                }).ToArray())
            };

            Task<MessageJsonModel> task;
            if (properties.Attachments.HasValue)
            {
                // If there is an attachment, we must send it via multipart HTTP content.
                // Our `messageContent` will be serialized into a "payload_json" form data field.
                var content = new MultipartJsonPayloadRestRequestContent<ModifyWebhookMessageJsonRestRequestContent>(messageContent, properties.Attachments.Value);
                task = messageId == null
                    ? client.ApiClient.ModifyInitialInteractionResponseAsync(applicationId, interactionToken, content, options, cancellationToken)
                    : client.ApiClient.ModifyFollowupInteractionResponseAsync(applicationId, interactionToken, messageId.Value, content, options, cancellationToken);
            }
            else
            {
                task = messageId == null
                    ? client.ApiClient.ModifyInitialInteractionResponseAsync(applicationId, interactionToken, messageContent, options, cancellationToken)
                    : client.ApiClient.ModifyFollowupInteractionResponseAsync(applicationId, interactionToken, messageId.Value, messageContent, options, cancellationToken);
            }

            var model = await task.ConfigureAwait(false);
            return new TransientUserMessage(client, model);
        }

        public static Task DeleteInteractionFollowupAsync(this IRestClient client,
            Snowflake applicationId, string interactionToken, Snowflake messageId,
            IRestRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.ApiClient.DeleteFollowupInteractionResponseAsync(applicationId, interactionToken, messageId, options, cancellationToken);
        }
    }
}
