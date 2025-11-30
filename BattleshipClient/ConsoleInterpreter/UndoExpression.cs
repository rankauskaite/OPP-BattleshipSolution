using System;

namespace BattleshipClient.ConsoleInterpreter
{
    /// <summary>
    /// Komanda: undo
    /// </summary>
    public sealed class UndoExpression : IExpression
    {
        public bool CanInterpret(ConsoleContext context)
        {
            return context.Tokens.Length == 1 &&
                   context.Tokens[0].Equals("undo", StringComparison.OrdinalIgnoreCase);
        }

        public void Interpret(ConsoleContext context)
        {
            var form = context.Form;

            if (form.InvokeRequired)
            {
                form.BeginInvoke(new Action(() => Interpret(context)));
                return;
            }

            if (!form.CommandManager.CanUndo)
            {
                Console.WriteLine("Nėra ką atšaukti.");
                return;
            }

            form.CommandManager.Undo();
            Console.WriteLine("Paskutinė komanda atšaukta.");
        }
    }
}
