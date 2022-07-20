using Rocket.Core.Logging;
using Rocket.Core.Utils;
using SDG.Unturned;
using SteamServerQuery;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnturnedBlackout.Managers
{
    public class ServerManager
    {
        private bool Continue { get; set; }

        public ServerManager()
        {
            Continue = true;
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await CheckServersAsync();
            });
        }

        public void Destroy()
        {
            Continue = false;
        }

        public async Task CheckServersAsync()
        {
            while (Continue)
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
                        if (server.IsOnline)
                        {
                            server.LastOnline = DateTime.UtcNow;
                            server.IsOnline = false;
                        }
                    }
                }

                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UIManager.OnServersUpdated());
            }
        }
    }
}
