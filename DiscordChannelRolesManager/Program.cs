using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordChannelRolesManager.Services;
using DiscordChannelRolesManager.TypeReaders;
using dotenv.net;
using dotenv.net.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DiscordChannelRolesManager
{
    public class Program
    {
        public static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Console.WriteLine(
                    $"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}"
            );
            Console.ResetColor();

            return Task.CompletedTask;
        }


        public static void Main(string[] args)
        {
            DotEnv.Load(options: new DotEnvOptions(trimValues: true));
            var envVars = DotEnv.Read();

            var dcToken = envVars["DiscordToken"];

            new Program().MainAsync(dcToken).GetAwaiter().GetResult();
        }


        private async Task MainAsync(string token)
        {
            await using var services = ConfigureServices();
            var client = services.GetRequiredService<DiscordSocketClient>();

            client.Log += Log;
            services.GetRequiredService<CommandService>().Log += Log;


            // Here we initialize the logic required to register our commands.
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            client.Ready += () =>
            {
                Log(new LogMessage(LogSeverity.Info, "Main", "Bot is connected!"));
                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }


        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                   .AddSingleton(
                           p => new DiscordSocketClient(
                                   new DiscordSocketConfig
                                   {
                                           MessageCacheSize = 100,
                                   }
                           )
                   )
                   .AddSingleton<CommandService>()
                   .AddSingleton<CommandHandlingService>()
                   .AddSingleton<HttpClient>()
                   .BuildServiceProvider();
        }
    }
}