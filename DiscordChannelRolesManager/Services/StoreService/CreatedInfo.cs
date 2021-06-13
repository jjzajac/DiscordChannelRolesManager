namespace DiscordChannelRolesManager.Services
{
    public class CreatedInfo
    {
        public ulong ChannelId { get; init; }
        public ulong MessageId { get; init; }
        public ulong? Cat { get; init; }
        public bool IsCatCreated { get; init; } = false;
    }
}