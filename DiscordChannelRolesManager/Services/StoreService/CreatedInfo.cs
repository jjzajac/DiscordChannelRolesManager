using System.Collections.Generic;

namespace DiscordChannelRolesManager.Services.StoreService
{
    public class CreatedInfo
    {
        public IList<ulong> ChannelsId { get; init; }
        public IList<ulong> RolesId { get; init; }
        public ulong? Cat { get; init; }
        public bool IsCatCreated { get; init; } = false;
    }
}