using System;
using System.Collections.Generic;

namespace BattleshipClient.ConsoleInterpreter
{
    public sealed class CommandInterpreter
    {
        private readonly List<IExpression> _expressions = new();

        public CommandInterpreter()
        {
            _expressions.Add(new HelpExpression());
            _expressions.Add(new ShootExpression());
            _expressions.Add(new PowerUpShootExpression());
            _expressions.Add(new UndoExpression());
            _expressions.Add(new RedoExpression());
        }

        public void Interpret(string input, MainForm form)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            var context = new ConsoleContext(input, form);

            foreach (var expr in _expressions)
            {
                if (expr.CanInterpret(context))
                {
                    expr.Interpret(context);
                    return;
                }
            }

            Console.WriteLine("Neatpažinta komanda. Parašykite 'help' dėl galimų komandų.");
        }
    }
}
