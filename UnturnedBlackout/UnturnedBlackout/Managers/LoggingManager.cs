using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using SDG.Unturned;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Managers;

public class LoggingManager
{
    public List<string> PendingWrite { get; set; }
    private Timer WriteTimer { get; set; }
    public string LogDir { get; set; }
    public string DumpDir { get; set; }

    private const string WARNINGS_URL = "https://discord.com/api/webhooks/1028305456524963960/hR907RCkg2gYkA-76x4n-qsK-SZY-GrzmJOppIkEmqzFwUrXnlvzTeyRQQdxp8gWh4Gv";

    public LoggingManager()
    {
        LogDir = Plugin.Instance.Directory + "/Logs/" + $"Log-{DateTime.Now:yyyy-MM-dd-hh:mm:ss}.txt";
        DumpDir = Plugin.Instance.Directory + "/Dumps/";

        var directoryName = Path.GetDirectoryName(LogDir);

        if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
            _ = Directory.CreateDirectory(directoryName);

        PendingWrite = new();
        WriteTimer = new(15 * 1000);
        WriteTimer.Elapsed += Write;
        WriteTimer.Start();
    }

    public void Dump(List<string> dumpContents)
    {
        var dump = DumpDir + $"Dump-{DateTime.Now:yyyy-MM-dd-hh:mm:ss}.txt";
        var directoryName = Path.GetDirectoryName(dump);

        if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
            _ = Directory.CreateDirectory(directoryName);

        File.AppendAllLines(dump, dumpContents);
    }

    public void Log(string message)
    {
        lock (PendingWrite)
            PendingWrite.Add(message);
    }

    public void Warn(string message)
    {
        Embed embed = new("", "Warning", "", "10038562", DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon), new(Provider.serverName, "", Provider.configData.Browser.Icon), new Field[] { new("Message:", message, true) }, null, null);

        Plugin.Instance.Discord.ForceSendEmbed(embed, "Warning", WARNINGS_URL);
    }

    private void Write(object sender, ElapsedEventArgs e)
    {
        if (PendingWrite.Count == 0)
            return;

        List<string> pending;
        lock (PendingWrite)
        {
            pending = PendingWrite.ToList();
            PendingWrite.Clear();
        }

        File.AppendAllLines(LogDir, pending);
    }
}