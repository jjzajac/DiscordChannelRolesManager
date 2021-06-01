using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Mime;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordChannelRolesManager.Modules
{
    public class Modules : ModuleBase<SocketCommandContext>
    {
        [Command("create")]
        public async Task CreateChannelAsync([Remainder] IDictionary<string, string> dict)
        {
            var cat = Context.Guild.CategoryChannels.Where(c => c.Name == "przedmioty").ToList();
            var catId = cat.Any()
                    ? cat.First().Id
                    : (await Context.Guild.CreateCategoryChannelAsync("przedmioty")).Id;

            foreach (var name in dict.Values)
            {
                var roles = Context.Guild.Roles.Where(c => c.Name == name).ToList();
                var roleId = roles.Any()
                        ? roles.First().Id
                        : (await Context.Guild.CreateRoleAsync(
                                name, GuildPermissions.None, Color.Default, false, true
                        )).Id;


                var channel = await Context.Guild.CreateTextChannelAsync(
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
                var mess = await Context.Message.Channel.SendMessageAsync($"@everyone {name}");
            }

            string s = dict.Aggregate("", (current, e) => current + $"- {e.Key} -> {e.Value} \n");
            var b = new EmbedBuilder
            {
                    Title = "Dodaj reakcje pod tym postem:",
                    Description = s,
            }.Build();
            await Context.Channel.SendMessageAsync(embed: b);
        }

        [Command("dict")]
        public async Task DictTestAsync([Remainder] IDictionary<string, string> dict)
        {
            string s = dict.Aggregate("", (current, e) => current + $"- {e.Key} -> {e.Value} \n");

            var b = new EmbedBuilder
            {
                    Title = "Dodaj reakcje pod tym postem:",
                    Description = s,
            }.Build();
            await Context.Channel.SendMessageAsync(embed: b);
        }
    }
}