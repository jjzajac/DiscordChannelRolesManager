using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordChannelRolesManager.Helpers.TypeReaders
{
    public class DictionaryTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(
                ICommandContext context,
                string input,
                IServiceProvider services
        )
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            var b = input.Split(" ").Where(s => s != "").ToArray();
            if (b.Length % 2 != 0)
            {
                return Task.FromResult(
                        TypeReaderResult.FromError(
                                CommandError.ParseFailed, "Couldn't parse to dictionary. Length is not even."
                        )
                );
            }

            for (var i = 0; i < b.Length; i += 2)
            {
                result.Add(b[i], b[i + 1]);
            }

            return Task.FromResult(TypeReaderResult.FromSuccess(result));
        }
    }
}