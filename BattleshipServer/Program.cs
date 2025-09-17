using System;
using System.Threading.Tasks;

namespace BattleshipServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int port = 5000;
            if (args.Length > 0) int.TryParse(args[0], out port);

            var server = new Server();
            await server.StartAsync(port);
        }
    }
}
