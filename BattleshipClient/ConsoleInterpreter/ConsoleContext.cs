using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleshipClient.ConsoleInterpreter
{
    public sealed class ConsoleContext
    {
        public BattleshipClient.MainForm Form { get; }
        public string Input { get; }
        public IReadOnlyList<string> Tokens { get; }

        public string output { get; private set; } = "";

        public Action<string> Output { get; }

        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public bool UsePlus { get; set; }
        public bool UseX { get; set; }
        public bool UseSuper { get; set; }
        public bool ShouldExit { get; set; }

        public ConsoleContext(string input, BattleshipClient.MainForm form, Action<string>? output = null)
        {
            Input = input ?? "";
            Form = form ?? throw new ArgumentNullException(nameof(form));
            var sink = output ?? Console.WriteLine;
            Output = msg =>
            {
                this.output += msg + Environment.NewLine;
                sink(msg);
            };

   
            var norm = Input.Replace(";", " ; ");
            Tokens = norm.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.Trim())
                         .ToList();
        }

        public void Ui(Action action)
        {
            if (Form.InvokeRequired) Form.BeginInvoke(action);
            else action();
        }
    }
}
