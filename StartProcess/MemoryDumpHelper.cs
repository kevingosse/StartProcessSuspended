// <copyright file="MemoryDumpHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Diagnostics;

namespace StartProcess
{
    public static class MemoryDumpHelper
    {
        public static async Task<Task> MonitorCrashes(int pid)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var procdumpStartedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = Task.Factory.StartNew(
                () =>
                {
                    var args = $"-ma -accepteula -e {pid} C:\\TempProcdump";

                    using var dumpToolProcess = Process.Start(new ProcessStartInfo(Program.ProcdumpPath, args)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    })!;

                    const string procdumpStarted = "Press Ctrl-C to end monitoring without terminating the process.";

                    void OnDataReceived(string output)
                    {
                        if (output == procdumpStarted)
                        {
                            procdumpStartedTcs.TrySetResult(true);
                        }
                    }

                    using var helper = new ProcessHelper(dumpToolProcess, OnDataReceived);

                    helper.Drain();

                    if (helper.StandardOutput.Contains("Dump count reached") || !helper.StandardOutput.Contains("Dump count not reached"))
                    {
                        Console.WriteLine($"[dump] procdump for process {pid} exited with code {helper.Process.ExitCode}");

                        Console.WriteLine($"[dump][stdout] {helper.StandardOutput}");
                        Console.WriteLine($"[dump][stderr] {helper.ErrorOutput}");

                        tcs.TrySetCanceled();
                    }

                    tcs.TrySetResult(true);
                },
                TaskCreationOptions.LongRunning);

            await procdumpStartedTcs.Task;

            return tcs.Task;
        }
    }
}
