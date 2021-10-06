using System.Collections.Generic;

namespace DiscordChannelRolesManager.Services.StoreService
{
    public class InMemoryStoreService : IStoreCreatedInfoService
    {
        private readonly Dictionary<KeyLabelGuild, CreatedInfo> _dict;

        public class KeyLabelGuild
        {
            public string Label { get; init; }
            public ulong Guild { get; init; }


            public override string ToString()
            {
                return $"{Guild}-{Label}";
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as KeyLabelGuild);
            }

            public bool Equals(KeyLabelGuild? obj)
            {
                return obj != null && obj.Label == Label && obj.Guild == Guild;
            }

            public override int GetHashCode()
            {
                return $"{Guild}-{Label}".GetHashCode();
            }
        }

        public InMemoryStoreService()
        {
            _dict = new Dictionary<KeyLabelGuild, CreatedInfo>();
        }

        public void AddCreatedInfoLabel(string label, ulong guild, CreatedInfo info)
        {
            _dict.Add(new KeyLabelGuild {Label = label, Guild = guild}, info);
        }

        public void RemoveCreatedInfo(string label, ulong guild)
        {
            _dict.Remove(new KeyLabelGuild {Label = label, Guild = guild});
        }

        public bool TryGetCreatedInfo(string label, ulong guild, out CreatedInfo? info)
        {
            return _dict.TryGetValue(new KeyLabelGuild {Label = label, Guild = guild}, out info);
        }
    }
}