using Rocket.Core.Utils;
using SDG.Unturned;
using SteamServerQuery;
using System.Threading;
using System.Threading.Tasks;

namespace UnturnedBlackout.Managers
{
    public class ServerManager
    {
        public ServerManager()
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await CheckServersAsync();
            });
        }

        public async Task CheckServersAsync()
        {
            while (Provider.isServer)
            {
                await Task.Delay(30 * 1000);

                Logging.Debug($"Checking registered servers");
                foreach (var server in Plugin.Instance.DBManager.Servers)
                {
                    Logging.Debug($"Checking server with ip: {server.IP}, port: {server.Port}");
                    try
                    {
                        Logging.Debug("Connecting");
                        ServerInfo info = await SteamServer.QueryServerAsync(server.IP, server.PortNo, 1000);
                        Logging.Debug($"Got server info with players: {info.Players}, max players: {info.MaxPlayers}, name: {info.Name}");
                        server.Players = info.Players;
                        server.MaxPlayers = info.MaxPlayers;
                        server.Name = info.Name;
                        server.IsOnline = true;
                    }
                    catch
                    {
                        Logging.Debug("Error connecting to server, setting online to false");
                        server.IsOnline = false;
                    }
                }

                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UIManager.OnServersUpdated());
            }
        }
    }
}
