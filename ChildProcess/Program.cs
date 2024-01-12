using System.Diagnostics;

namespace ChildProcess;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"Hello, World! - {Process.GetCurrentProcess().Id}");
    }
}
