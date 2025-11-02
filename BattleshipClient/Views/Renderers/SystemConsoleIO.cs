using System;
using System.Runtime.InteropServices;

namespace BattleshipClient.Views.Renderers
{
    public sealed class SystemConsoleIO : IAsciiConsole
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        private static bool _consoleReady;

        public SystemConsoleIO()
        {
            if (!_consoleReady)
            {
                try { AllocConsole(); } catch { /* ignore */ }
                _consoleReady = true;
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.Clear();
            }
        }

        public void Put(int x, int y, char ch)
        {
            try
            {
                Console.SetCursorPosition(x, y);
                Console.Write(ch);
            }
            catch { /* mažos konsolės atveju ignoruojam */ }
        }

        public void WriteText(int x, int y, string text)
        {
            try
            {
                Console.SetCursorPosition(x, y);
                Console.Write(text);
            }
            catch { /* ignoruojam */ }
        }
    }
}
