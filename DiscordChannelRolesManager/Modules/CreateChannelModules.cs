using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using DiscordChannelRolesManager.Helpers.TypeReaders;
using DiscordChannelRolesManager.Services;

namespace DiscordChannelRolesManager.Modules
{
    public class CreateChannelModules : ModuleBase<SocketCommandContext>
    {
        private readonly IStoreCreatedInfoService _storeCreatedInfoService;

        public CreateChannelModules(IStoreCreatedInfoService storeCreatedInfoService)
        {
            _storeCreatedInfoService = storeCreatedInfoService;
        }

        [Command("create")]
        public async Task CreateChannelAsync([Remainder] IDictionary<string, string> dict)
        {
            dict.SplitOptionsParams(out var options, out var rest);

            ulong? catId = null;
            var catExist = true;
            if (options.TryGetValue(":cat", out var catVal))
            {
                var cat = Context.Guild.CategoryChannels.Where(c => c.Name == catVal).ToList();
                catExist = cat.Any();
                catId = cat.Any()
                        ? cat.First().Id
                        : (await Context.Guild.CreateCategoryChannelAsync(catVal)).Id;
            }


            foreach (var name in rest.Values)
            {
                var roles = Context.Guild.Roles.Where(c => c.Name == name).ToList();
                var roleId = roles.Any()
                        ? roles.First().Id
                        : (await Context.Guild.CreateRoleAsync(
                                name, GuildPermissions.None, Color.Default, false, true
                        )).Id;


                var channel = await CreateChannelWithRoleAsync(name, catId, roleId);
            }

            var res = await SendResponseWithReactions(rest);
            if (options.TryGetValue(":label", out var label))
            {
                _storeCreatedInfoService.AddCreatedInfoLabel(
                        label, Context.Guild.Id, new CreatedInfo
                        {
                                Cat = catId,
                                IsCatCreated = !catExist,
                                ChannelId = Context.Channel.Id,
                                MessageId = res.Id,
                        }
                );
            }
        }

        private async Task<RestUserMessage> SendResponseWithReactions(Dictionary<string, string> rest)
        {
            string s = rest.Aggregate("", (current, e) => current + $"- {e.Key} -> {e.Value}\n");
            var b = new EmbedBuilder
            {
                    Title = "Dodaj reakcje pod tym postem:",
                    Description = s,
            }.Build();
            var res = await Context.Channel.SendMessageAsync(embed: b);
            foreach (var keyValuePair in rest)
            {
                await res.AddReactionAsync(new Emoji(keyValuePair.Key));
            }

            return res;
        }

        private async Task<RestTextChannel> CreateChannelWithRoleAsync(string name, ulong? catId, ulong roleId)
        {
            return await Context.Guild.CreateTextChannelAsync(
                    name,
                    c =>
                    {
                        c.CategoryId = catId;
                        c.PermissionOverwrites = new List<Overwrite>
                        {
                                new(
                                        Context.Guild.EveryoneRole.Id,
                                        PermissionTarget.Role,
                                        new(viewChannel: PermValue.Deny)
                                ),
                                new(
                                        roleId,
                                        PermissionTarget.Role,
                                        new(viewChannel: PermValue.Allow)
                                ),
                        };
                    }
            );
        }

        [Command("rem")]
        public async Task CreateChannelAsync(string label)
        {
            if (_storeCreatedInfoService.TryGetCreatedInfo(label, Context.Guild.Id, out var info))
            {
                await Context.Channel.SendMessageAsync("Test");
            }
        }
    }
}