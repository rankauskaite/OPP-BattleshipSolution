using System;

namespace BattleshipClient.ConsoleInterpreter
{
    /// <summary>
    /// Komandos: plus B5, x C3, super A1
    /// </summary>
    public sealed class PowerUpShootExpression : IExpression
    {
        public bool CanInterpret(ConsoleContext context)
        {
            if (context.Tokens.Length < 2) return false;

            var verb = context.Tokens[0];
            return verb.Equals("plus", StringComparison.OrdinalIgnoreCase)
                || verb.Equals("x", StringComparison.OrdinalIgnoreCase)
                || verb.Equals("super", StringComparison.OrdinalIgnoreCase);
        }

        public void Interpret(ConsoleContext context)
        {
            if (context.Tokens.Length < 2)
            {
                Console.WriteLine("Naudojimas: plus B5 | x C3 | super A1");
                return;
            }

            string verb = context.Tokens[0].ToLowerInvariant();
            string coordToken = context.Tokens[1].ToUpperInvariant();

            if (!TryParseCoordinate(coordToken, out int x, out int y))
            {
                Console.WriteLine("Blogas koordinatės formatas. Naudokite pvz.: B5, C10 ir pan.");
                return;
            }

            bool usePlus = verb == "plus";
            bool useX = verb == "x";
            bool useSuper = verb == "super";

            context.Form.FireShotFromConsole(x, y, usePlus, useX, useSuper);
        }

        private static bool TryParseCoordinate(string coord, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (string.IsNullOrWhiteSpace(coord) || coord.Length < 2)
                return false;

            char colChar = coord[0];
            if (colChar < 'A' || colChar > 'Z')
                return false;

            if (!int.TryParse(coord.Substring(1), out int row))
                return false;

            x = colChar - 'A';
            y = row - 1;

            return true;
        }
    }
}
