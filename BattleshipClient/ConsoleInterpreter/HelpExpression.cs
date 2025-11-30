using System;

namespace BattleshipClient.ConsoleInterpreter
{
    /// <summary>
    /// Komanda: help / ?
    /// </summary>
    public sealed class HelpExpression : IExpression
    {
        public bool CanInterpret(ConsoleContext context)
        {
            if (context.Tokens.Length == 0) return true;

            var verb = context.Tokens[0];
            return verb.Equals("help", StringComparison.OrdinalIgnoreCase)
                || verb.Equals("?", StringComparison.OrdinalIgnoreCase);
        }

        public void Interpret(ConsoleContext context)
        {
            Console.WriteLine("Galimos komandos:");
            Console.WriteLine("  shoot B5   - paprastas šūvis");
            Console.WriteLine("  plus C7    - + formos power-up šūvis");
            Console.WriteLine("  x D4       - X formos power-up šūvis");
            Console.WriteLine("  super A1   - super šūvis");
            Console.WriteLine("  undo       - atšaukti paskutinę lentos būsenos komandą (jei įmanoma)");
            Console.WriteLine("  redo       - pakartoti atšauktą komandą");
            Console.WriteLine("  help / ?   - pagalbos tekstas");
            Console.WriteLine("  exit       - išeiti iš konsolės režimo");
        }
    }
}
