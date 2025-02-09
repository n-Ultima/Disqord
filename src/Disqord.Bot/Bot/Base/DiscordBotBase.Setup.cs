using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Parsers;
using Microsoft.Extensions.Logging;
using Qmmands;

using Module = Qmmands.Module;

namespace Disqord.Bot
{
    public abstract partial class DiscordBotBase
    {
        protected virtual IEnumerable<Assembly> GetModuleAssemblies()
            => new[] { Assembly.GetEntryAssembly() };

        protected virtual bool CheckModule(TypeInfo type)
            => true;

        protected virtual void MutateModule(ModuleBuilder moduleBuilder)
        {
            var methods = moduleBuilder.Type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            var method = methods.FirstOrDefault(x => x.GetCustomAttribute<MutateModuleAttribute>() != null);
            if (method == null)
                return;

            method.Invoke(null, new object[] { moduleBuilder });
        }

        protected virtual ValueTask AddTypeParsersAsync(CancellationToken cancellationToken = default)
        {
            Commands.AddTypeParser(new SnowflakeTypeParser());
            Commands.AddTypeParser(new ColorTypeParser());
            Commands.AddTypeParser(new CustomEmojiTypeParser());
            Commands.AddTypeParser(new GuildEmojiTypeParser());
            Commands.AddTypeParser(new GuildChannelTypeParser<IGuildChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<ICategorizableGuildChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<IMessageGuildChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<IVocalGuildChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<ITextChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<IVoiceChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<ICategoryChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<IStageChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<IThreadChannel>());
            Commands.AddTypeParser(new GuildChannelTypeParser<IStoreChannel>());
            Commands.AddTypeParser(new MemberTypeParser());
            Commands.AddTypeParser(new RoleTypeParser());
            return default;
        }

        protected virtual ValueTask AddModulesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var modules = new List<Module>();
                foreach (var assembly in GetModuleAssemblies())
                {
                    modules.AddRange(Commands.AddModules(assembly, CheckModule, MutateModule));
                }

                Logger.LogInformation("Added {0} command modules with {1} commands.", modules.Count, modules.SelectMany(CommandUtilities.EnumerateAllCommands).Count());
            }
            catch (CommandMappingException ex)
            {
                Logger.LogCritical(ex, "Failed to map command {0} in module {1}:", ex.Command, ex.Command.Module);
                throw;
            }

            return default;
        }

        public virtual async ValueTask SetupAsync(CancellationToken cancellationToken = default)
        {
            _masterService.Bind(this);

            await AddTypeParsersAsync(cancellationToken).ConfigureAwait(false);
            await AddModulesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
