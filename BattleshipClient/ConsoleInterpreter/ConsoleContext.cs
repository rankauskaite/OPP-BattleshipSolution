using System;
using BattleshipClient;

namespace BattleshipClient.ConsoleInterpreter
{
    public sealed class ConsoleContext
    {
        public string Input { get; }
        public string[] Tokens { get; }
        public MainForm Form { get; }

        public ConsoleContext(string input, MainForm form)
        {
            Input = input ?? string.Empty;
            Tokens = Input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Form = form ?? throw new ArgumentNullException(nameof(form));
        }
    }
}
