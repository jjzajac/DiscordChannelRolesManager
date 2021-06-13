namespace DiscordChannelRolesManager.Services
{
    public interface IStoreCreatedInfoService
    {
        public void AddCreatedInfoLabel(string label, ulong guild, CreatedInfo info);

        public void RemoveCreatedInfo(string label, ulong guild);

        public bool TryGetCreatedInfo(string label, ulong guild, out CreatedInfo? info);
    }
}