using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordChannelRolesManager.Helpers.TypeReaders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordChannelRolesManager.Services
{
    public class CommandHandler
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(
                CommandService commands, IServiceProvider services, ILogger<CommandHandler> logger,
                DiscordSocketClient client
        )
        {
            _commands = commands;
            _services = services;
            _logger = logger;
            _client = client;

            _commands.CommandExecuted += CommandExecutedAsync;

            _client.MessageReceived += MessageReceivedAsync;
            _client.ReactionAdded += ReactionAdded;
        }

        public async Task InitializeAsync()
        {
            _commands.AddTypeReader(typeof(IDictionary<string, string>), new DictionaryTypeReader());
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }


        private async Task ReactionAdded(
                Cacheable<IUserMessage, ulong> cachedMessage,
                ISocketMessageChannel originChannel,
                SocketReaction reaction
        )
        {
            if (cachedMessage.Value.Author.IsBot &&
                cachedMessage.Value.Author.DiscriminatorValue == 2666)
            {
                var dd = cachedMessage.Value.Embeds.First().Description
                                      .Split("\n")
                                      .Select(
                                              line =>
                                              {
                                                  var l = line[2..].Split(" -> ");
                                                  return new KeyValuePair<string, string>(l[0], l[1]);
                                              }
                                      )
                                      .ToDictionary(pair => pair.Key, pair => pair.Value);

                if (dd.TryGetValue(reaction.Emote.Name, out var s))
                {
                    var context = new CommandContext(
                            _services.GetService<DiscordSocketClient>(), cachedMessage.Value
                    );
                    var gu = await context.Guild.GetUserAsync(reaction.UserId);
                    var role = context.Guild.Roles.First(r => r.Name == s);
                    await gu.AddRoleAsync(role);
                }
            }
        }


        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (rawMessage is not SocketUserMessage {Source: MessageSource.User} message)
                return;
            _logger.LogDebug($"MessageReceivedHandler {rawMessage.Content}");

            var argPos = 0;
            if (!message.HasCharPrefix('!', ref argPos)) return;
            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (result.IsSuccess)
            {
                _logger.LogInformation($"MessageReceivedHandler {result}");
            }
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified || result.IsSuccess)
                return;

            _logger.LogError($"CommandExecutedAsync {result}");
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}