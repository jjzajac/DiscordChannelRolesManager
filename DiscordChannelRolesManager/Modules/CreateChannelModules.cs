using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using DiscordChannelRolesManager.Helpers.TypeReaders;

namespace DiscordChannelRolesManager.Modules
{
    public class CreateChannelModules : ModuleBase<SocketCommandContext>
    {
        [Command("create")]
        public async Task CreateChannelAsync([Remainder] IDictionary<string, string> dict)
        {
            dict.SplitOptionsParams(out var options, out var rest);

            ulong? catId = null;
            if (options.TryGetValue(":cat", out var catVal))
            {
                var cat = Context.Guild.CategoryChannels.Where(c => c.Name == catVal).ToList();
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

            await SendResponseWithReactions(rest);
        }

        private async Task SendResponseWithReactions(Dictionary<string, string> rest)
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

        private CreateChannelModules() { }
    }
}