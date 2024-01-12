using System.Diagnostics;

namespace StartProcess;

internal class Program
{
    internal const string ProcdumpPath = @"C:\TempProcdump\procdump.exe";

    static async Task Main(string[] args)
    {
        const string path = @"..\..\..\..\ChildProcess\bin\x86\Release\net8.0\ChildProcess.exe";

        int total = 0;

        var cts = new CancellationTokenSource();

        int nbThreads = Environment.ProcessorCount;

        var tasks = new Task[nbThreads];

        for (int i = 0; i < nbThreads; i++)
        {
            tasks[i] = Task.Factory.StartNew(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var iteration = Interlocked.Increment(ref total);

                    if (iteration % 10 == 0)
                    {
                        Console.WriteLine($"Iteration {iteration}");
                    }

                    var startInfo = new ProcessStartInfo(path);

                    try
                    {
                        await StartProcess(startInfo, false);
                    }
                    catch (Exception)
                    {
                        cts.Cancel();
                    }
                }
            }, TaskCreationOptions.LongRunning).Unwrap();
        }

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await Task.WhenAll(tasks);
    }

    private static async Task StartProcess(ProcessStartInfo startInfo, bool redirectStandardInput)
    {
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = redirectStandardInput;

        using var suspendedProcess = NativeProcess.CreateProcess.StartSuspendedProcess(startInfo);

        var task = await MemoryDumpHelper.MonitorCrashes(suspendedProcess.Id);

        using var process = suspendedProcess.ResumeProcess();

        await task;
    }
}
