using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordChannelRolesManager.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace DiscordChannelRolesManager
{
    public sealed class EntryPoint : IHostedService
    {
        private readonly ILogger<EntryPoint> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly CommandHandler _command;
        private readonly IServiceProvider _service;

        private int? _exitCode;

        public EntryPoint(
                ILogger<EntryPoint> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration,
                DiscordSocketClient client, CommandHandler command, IServiceProvider service
        )
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _configuration = configuration;
            _client = client;
            _command = command;
            _service = service;
        }

        private async Task Run()
        {
            try
            {
                ActivatorUtilities.GetServiceOrCreateInstance<LoggingService>(_service);
                await _client.LoginAsync(TokenType.Bot, _configuration["DiscordToken"]);
                await _client.StartAsync();

                await _command.InitializeAsync();

                await Task.Delay(-1);
                
                _exitCode = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception!");
                _exitCode = 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Starting with arguments: {string.Join(" ", Environment.GetCommandLineArgs())}");

            _appLifetime.ApplicationStarted.Register(() => Task.Run(async () => await Run(), cancellationToken));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Exiting with return code: {_exitCode}");

            Environment.ExitCode = _exitCode.GetValueOrDefault(-1);
            return Task.CompletedTask;
        }
    }
}