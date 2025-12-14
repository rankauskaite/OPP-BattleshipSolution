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
        public Action<string> Output { get; }

        // rezultatai po interpretavimo
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
            Output = output ?? Console.WriteLine;

            // leidžiam "plus B5; undo"
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
