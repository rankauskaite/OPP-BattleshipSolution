using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BattleshipClient
{
    public static class ProxyBenchmark
    {
        public static async Task MeasureAsync(
            INetworkClient client,
            string uri,
            string label,
            int messageCount)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(false);

            var swConnect = Stopwatch.StartNew();
            await client.ConnectAsync(uri);
            swConnect.Stop();

            // Warm-up
            await client.SendAsync(new { type = "bench", payload = new { index = -1 } });

            var swSend = Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                var msg = new { type = "bench", payload = new { index = i } };
                await client.SendAsync(msg);
            }
            swSend.Stop();

            long memAfter = GC.GetTotalMemory(false);
            long memDelta = memAfter - memBefore;

            var report =
                $"{label}: {messageCount} msgs\n" +
                $"  connect: {swConnect.ElapsedMilliseconds} ms\n" +
                $"  send:    {swSend.ElapsedMilliseconds} ms\n" +
                $"  mem Δ:   {memDelta / 1024.0:0.0} KB\n";

            File.AppendAllText("benchmark_results.txt", report + Environment.NewLine);
            Console.WriteLine(report);
        }
    }
}
