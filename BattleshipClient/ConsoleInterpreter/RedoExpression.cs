using System;

namespace BattleshipClient.ConsoleInterpreter
{
    /// <summary>
    /// Komanda: redo
    /// </summary>
    public sealed class RedoExpression : IExpression
    {
        public bool CanInterpret(ConsoleContext context)
        {
            return context.Tokens.Length == 1 &&
                   context.Tokens[0].Equals("redo", StringComparison.OrdinalIgnoreCase);
        }

        public void Interpret(ConsoleContext context)
        {
            var form = context.Form;

            if (form.InvokeRequired)
            {
                form.BeginInvoke(new Action(() => Interpret(context)));
                return;
            }

            if (!form.CommandManager.CanRedo)
            {
                Console.WriteLine("Nėra ką pakartoti.");
                return;
            }

            form.CommandManager.Redo();
            Console.WriteLine("Atšaukta komanda pakartota.");
        }
    }
}
