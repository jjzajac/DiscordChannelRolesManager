using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordChannelRolesManager.Helpers.TypeReaders
{
    public static class DictionaryTypeReaderExtensions
    {
        public static void SplitOptionsParams(
                this IDictionary<string, string> dict,
                out Dictionary<string, string> parameters,
                out Dictionary<string, string> rest
        )
        {
            parameters = dict.Where(el => el.Key.StartsWith(":"))
                             .ToDictionary(el => el.Key, el => el.Value);
            
            rest = dict.Where(el => !el.Key.StartsWith(":"))
                       .ToDictionary(el => el.Key, el => el.Value);
        }
    }
}