using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Rocket.Core.Logging;

namespace UnturnedBlackout.Dispatcher;

public sealed class ActionScheduler : IDisposable
{
    private Queue<KeyValuePair<long, Func<Task>>> SecondThreadQueue { get; }
    private BackgroundWorker Worker { get; }
    private AutoResetEvent WorkerFinish { get; }
    private bool IsDisposed { get; set; }

    public ActionScheduler()
    {
        SecondThreadQueue = new();
        Worker = new() { WorkerSupportsCancellation = true };
        Worker.DoWork += ProcessQueue;
        WorkerFinish = new(true);
        IsDisposed = false;

        Worker.RunWorkerAsync();
    }

    private void ProcessQueue(object sender, DoWorkEventArgs e)
    {
        WorkerFinish.Reset();

        while (!Worker.CancellationPending && !IsDisposed)
        {
            try
            {
                if (SecondThreadQueue.Count == 0)
                    continue;

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var element = SecondThreadQueue.Dequeue();

                if (element.Key <= now)
                {
                    element.Value();
                    continue;
                }

                SecondThreadQueue.Enqueue(element);
            }
            catch (Exception ex)
            {
                Logger.Log("Action Scheduler threw an exception");
                Logger.Log(ex);
            }
        }

        WorkerFinish.Set();
    }

    public void QueueOnSecondThread(Func<Task> action, int actionDelay = 0)
    {
        if (IsDisposed)
            throw new ObjectDisposedException("UnturnedBlackout.Dispatcher.ActionScheduler");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SecondThreadQueue.Enqueue(new(now + actionDelay, action));
    }

    public void Dispose()
    {
        Worker.CancelAsync();

        if (Worker.IsBusy)
            WorkerFinish.WaitOne();

        Worker.Dispose();

        SecondThreadQueue.Clear();
        IsDisposed = true;
    }
}
