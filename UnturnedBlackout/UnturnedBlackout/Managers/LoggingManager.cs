using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace UnturnedBlackout.Managers;

public class LoggingManager
{
    public List<string> PendingWrite { get; set; }
    private Timer WriteTimer { get; set; }
    public string LogDir { get; set; }
    public string DumpDir { get; set; }
    
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