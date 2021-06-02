using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using DiscordChannelRolesManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DiscordChannelRolesManager
{
    public static class Startup
    {
        public static void ServicesCollection(IServiceCollection services)
        {
            services.AddHostedService<EntryPoint>()
                    .RegisterServices();
        }

        private static void RegisterServices(this IServiceCollection services)
            => services
               .AddSingleton(
                       _ => new DiscordSocketClient(
                               new DiscordSocketConfig
                               {
                                       MessageCacheSize = 100,
                               }
                       )
               )
               .AddSingleton<CommandService>()
               .AddSingleton<CommandHandler>()
               .AddSingleton<LoggingService>();
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                         .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
                         .MinimumLevel.Debug()
                         .Enrich.FromLogContext()
                         .CreateLogger();

            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                      .ConfigureServices(Startup.ServicesCollection)
                      .UseSerilog()
                      .RunConsoleAsync();
        }
    }
}