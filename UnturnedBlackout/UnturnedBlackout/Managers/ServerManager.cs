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
                await Task.Delay(10 * 1000);

                foreach (var server in Plugin.Instance.DBManager.Servers)
                {
                    try
                    {
                        ServerInfo info = await SteamServer.QueryServerAsync(server.IP, server.PortNo, 1000);
                        server.Players = info.Players;
                        server.MaxPlayers = info.MaxPlayers;
                        server.Name = info.Name;
                        server.IsOnline = true;
                    }
                    catch
                    {
                        server.IsOnline = false;
                    }
                }

                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UIManager.OnServersUpdated());
            }
        }
    }
}
