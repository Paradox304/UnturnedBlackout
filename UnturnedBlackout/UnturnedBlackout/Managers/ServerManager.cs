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
            Logging.Debug("Initializing server manager");
            Continue = true;
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                Logging.Debug("Starting checking servers");
                await CheckServersAsync();
            });
        }

        public void Destroy()
        {
            Continue = false;
        }

        public async Task CheckServersAsync()
        {
            Logging.Debug($"Started checking servers, continue: {Continue}");
            while (Continue)
            {
                await Task.Delay(10 * 1000);

                Logging.Debug($"Checking servers, got {Plugin.Instance.DBManager.Servers.Count} servers to check from");
                foreach (var server in Plugin.Instance.DBManager.Servers)
                {
                    Logging.Debug($"Checking server with IP: {server.IP} and Port: {server.PortNo}");
                    try
                    {
                        ServerInfo info = await SteamServer.QueryServerAsync(server.IP, server.PortNo, 1000);
                        Logging.Debug($"Server is online, getting the details, players: {info.Players}, max players: {info.MaxPlayers}, name: {info.Name}");
                        server.Players = info.Players;
                        server.MaxPlayers = info.MaxPlayers;
                        server.Name = info.Name;
                        server.IsOnline = true;
                    }
                    catch
                    {
                        Logging.Debug("Caught an exception, probably server is offline");
                        if (server.IsOnline)
                        {
                            Logging.Debug("Server was previously online, setting last online to the current time and moving on");
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
