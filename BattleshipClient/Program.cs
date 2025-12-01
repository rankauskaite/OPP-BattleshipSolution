using System;
using System.Windows.Forms;

namespace BattleshipClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Func<INetworkClient> factory = () => new NetworkClient();

            bool useProxy = false; 

            INetworkClient client = useProxy
                ? new NetworkClientProxy(factory, "localhost")
                : factory();

            Application.Run(new MainForm(client));
        }
    }
}
