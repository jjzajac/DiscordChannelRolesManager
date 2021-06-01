using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordChannelRolesManager;
using DiscordChannelRolesManager.TypeReaders;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordChannelRolesManager.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _commands.CommandExecuted += CommandExecutedAsync;

            _discord.MessageReceived += MessageReceivedAsync;
            _discord.ReactionAdded += ReactionAdded;
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
                                                  var l = line[2..].TrimEnd().Split(" -> ");
                                                  return new KeyValuePair<string, string>(l[0], l[1]);
                                              }
                                      )
                                      .ToDictionary(
                                              keySelector: pair => pair.Key,
                                              elementSelector: pair => pair.Value
                                      );

                if (dd.TryGetValue(reaction.Emote.Name, out var s))
                {
                    var context = new CommandContext(
                            _services.GetService<DiscordSocketClient>(), cachedMessage.Value
                    );
                    var gu = await context.Guild.GetUserAsync(reaction.UserId);
                    var role = context.Guild.Roles.First(r => r.Name == s);
                    await gu.AddRoleAsync(role);
                    var m = $"{reaction.User.Value.Username} react with {s}";
                    await originChannel.SendMessageAsync($"Pong!\n{m}");
                }
            }
        }


        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (rawMessage is not SocketUserMessage {Source: MessageSource.User} message)
                return;
            await Program.Log(new LogMessage(LogSeverity.Debug, "MessageReceivedHandler", rawMessage.Content));
            // This value holds the offset where the prefix ends
            var argPos = 0;
            if (!message.HasCharPrefix('!', ref argPos)) return;
            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (result.IsSuccess)
            {
                await Program.Log(new LogMessage(LogSeverity.Info, "MessageReceivedHandler", $"{result}"));
            }
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {

            if (!command.IsSpecified || result.IsSuccess)
                return;


            await Program.Log(new LogMessage(LogSeverity.Error, "CommandExecutedAsync", $"{result}"));
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}