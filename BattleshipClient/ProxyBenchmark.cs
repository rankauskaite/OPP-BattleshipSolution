using System;
using System.Diagnostics;
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
            var sw = Stopwatch.StartNew();
            await client.ConnectAsync(uri);

            for (int i = 0; i < messageCount; i++)
            {
                var msg = new { type = "ping", payload = new { index = i } };
                await client.SendAsync(msg);
            }

            sw.Stop();
            long memoryBytes = GC.GetTotalMemory(true);

            Console.WriteLine(
                $"{label}: {messageCount} msgs, time = {sw.ElapsedMilliseconds} ms, " +
                $"memory = {memoryBytes / 1024.0:0.0} KB");
        }
    }
}
