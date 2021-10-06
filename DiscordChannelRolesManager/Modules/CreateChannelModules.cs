using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using DiscordChannelRolesManager.Helpers.TypeReaders;
using DiscordChannelRolesManager.Services;
using DiscordChannelRolesManager.Services.StoreService;

namespace DiscordChannelRolesManager.Modules
{
    public class CreateChannelModules : ModuleBase<SocketCommandContext>
    {
        private readonly IStoreCreatedInfoService _storeCreatedInfoService;

        public CreateChannelModules(IStoreCreatedInfoService storeCreatedInfoService)
        {
            _storeCreatedInfoService = storeCreatedInfoService;
        }

        private async Task<(ulong roleId, bool roleCreate)> GetOrCreateRole(string name)
        {
            var roles = Context.Guild.Roles.Where(c => c.Name == name).ToList();
            var roleExist = roles.Any();
            var roleId = roleExist
                    ? roles.First().Id
                    : (await Context.Guild.CreateRoleAsync(
                            name, GuildPermissions.None, Color.Default, false, true
                    )).Id;
            return (roleId, !roleExist);
        }

        private async Task<(ulong catId, bool catCreated)> GetOrCreateCategory(string catVal)
        {
            var cats = Context.Guild.CategoryChannels.Where(c => c.Name == catVal).ToList();
            var catExist = cats.Any();
            var catId = catExist
                    ? cats.First().Id
                    : (await Context.Guild.CreateCategoryChannelAsync(catVal)).Id;
            return (catId, !catExist);
        }

        [Command("create")]
        public async Task CreateChannelAsync([Remainder] IDictionary<string, string> dict)
        {
            dict.SplitOptionsParams(out var options, out var rest);

            ulong? catId = null;
            var catCreated = true;
            if (options.TryGetValue(":cat", out var catVal))
            {
                (catId, catCreated) = await GetOrCreateCategory(catVal);
            }

            var createdRolesId = new List<ulong>();
            var createdChannelsId = new List<ulong>();
            foreach (var name in rest.Values)
            {
                var (roleId, roleCreate) = await GetOrCreateRole(name);
                if (roleCreate) createdRolesId.Add(roleId);
                var channel = await CreateChannelWithRoleAsync(name, catId, roleId);
                createdChannelsId.Add(channel.Id);
            }

            var res = await SendResponseWithReactions(rest);
            if (options.TryGetValue(":label", out var label))
            {
                _storeCreatedInfoService.AddCreatedInfoLabel(
                        label,
                        Context.Guild.Id,
                        new CreatedInfo
                        {
                                Cat = catId,
                                IsCatCreated = catCreated,
                                ChannelsId = createdChannelsId,
                                RolesId = createdRolesId
                        }
                );
            }
        }

        [Command("rem")]
        public async Task CreateChannelAsync(string label)
        {
            if (_storeCreatedInfoService.TryGetCreatedInfo(label, Context.Guild.Id, out var info))
            {
                if (info is {IsCatCreated: true})
                {
                    var cats = Context.Guild.CategoryChannels.Single(cat => cat.Id == info.Cat);
                    Console.WriteLine("t");
                    cats?.DeleteAsync();
                }

                if (info?.RolesId != null)
                {
                    Context.Guild.Roles
                           .Where(role => info.ChannelsId.Contains(role.Id)).ToList()
                           .ForEach(
                                   async role =>
                                   {
                                       role.Members
                                           .ToList()
                                           .ForEach(async r => await r.RemoveRoleAsync(role));
                                       await role.DeleteAsync();
                                   }
                           );
                }

                if (info?.ChannelsId != null)
                {
                    Task.WaitAll(
                            Context.Guild.Channels
                                   .Where(chan => info.ChannelsId.Contains(chan.Id))
                                   .Select(chan => chan.DeleteAsync())
                                   .ToArray()
                    );
                }

                await Context.Channel.SendMessageAsync("Te st");

                _storeCreatedInfoService.RemoveCreatedInfo(label, Context.Guild.Id);
            }
        }


        private Embed RespReactions(string description) =>
                new EmbedBuilder
                {
                        Title = "Dodaj reakcje pod tym postem:",
                        Description = description,
                }.Build();

        private async Task<RestUserMessage> SendResponseWithReactions(Dictionary<string, string> rest)
        {
            string description = rest.Aggregate("", (current, e) => current + $"- {e.Key} -> {e.Value}\n");
            var res = await Context.Channel.SendMessageAsync(embed: RespReactions(description));

            foreach (var keyValuePair in rest)
            {
                await res.AddReactionAsync(new Emoji(keyValuePair.Key));
            }

            return res;
        }

        private async Task<RestTextChannel> CreateChannelWithRoleAsync(string name, ulong? catId, ulong roleId)
        {
            var objPermissionOverwrites = new List<Overwrite>
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

            return await Context.Guild.CreateTextChannelAsync(
                    name,
                    c =>
                    {
                        c.CategoryId = catId;
                        c.PermissionOverwrites = objPermissionOverwrites;
                    }
            );
        }
    }
}