using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BattleshipClient
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Func<INetworkClient> factory = () => new NetworkClient();

            // Benchmark režimas: dotnet run -- --bench
            if (args != null && args.Contains("--bench"))
            {
                RunBench(factory).GetAwaiter().GetResult();
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool useProxy = false;

            INetworkClient client = useProxy
                ? new NetworkClientProxy(factory, "localhost")
                : factory();

            Application.Run(new MainForm(client));
        }

        private static async Task RunBench(Func<INetworkClient> factory)
        {
            // Serverio output rodo: http://localhost:5000/ws/
            string uri = "ws://localhost:5000/ws/";
            int n = 1000;

            await ProxyBenchmark.MeasureAsync(factory(), uri, "Direct NetworkClient", n);
            await ProxyBenchmark.MeasureAsync(new NetworkClientProxy(factory, "localhost"), uri, "NetworkClientProxy", n);
        }
    }
}
