using System;
using BattleshipClient.Commands;
using BattleshipClient.Iterators;

namespace BattleshipClient.ConsoleInterpreter
{
    // Komanda: history
    public sealed class HistoryExpression : IExpression
    {
        public bool CanInterpret(ConsoleContext context)
            => context.Tokens.Length == 1 &&
               context.Tokens[0].Equals("history", StringComparison.OrdinalIgnoreCase);

        public void Interpret(ConsoleContext context)
        {
            var form = context.Form;

            if (form.InvokeRequired)
            {
                form.BeginInvoke(new Action(() => Interpret(context)));
                return;
            }

            Print("UNDO", form.CommandManager.GetUndoHistory());
            Print("REDO", form.CommandManager.GetRedoHistory());
        }

        private static void Print(string title, IIterable<ICommand> iterable)
        {
            Console.WriteLine($"--- {title} history (nuo viršaus) ---");

            var it = iterable.GetIterator();
            int i = 0;

            while (it.MoveNext())
            {
                Console.WriteLine($"{++i}. {it.Current}");
            }

            if (i == 0) Console.WriteLine("(empty)");
        }
    }
}
