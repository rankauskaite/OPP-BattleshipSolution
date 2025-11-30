using System;

namespace BattleshipClient.ConsoleInterpreter
{
    /// <summary>
    /// Komanda: shoot B5 / s B5
    /// </summary>
    public sealed class ShootExpression : IExpression
    {
        public bool CanInterpret(ConsoleContext context)
        {
            if (context.Tokens.Length < 2) return false;

            var verb = context.Tokens[0];
            return verb.Equals("shoot", StringComparison.OrdinalIgnoreCase)
                || verb.Equals("s", StringComparison.OrdinalIgnoreCase);
        }

        public void Interpret(ConsoleContext context)
        {
            if (context.Tokens.Length < 2)
            {
                Console.WriteLine("Naudojimas: shoot B5");
                return;
            }

            string coordToken = context.Tokens[1].ToUpperInvariant();

            if (!TryParseCoordinate(coordToken, out int x, out int y))
            {
                Console.WriteLine("Blogas koordinatės formatas. Naudokite pvz.: B5, C10 ir pan.");
                return;
            }

            // Paprastas šūvis – be power-up’ų
            context.Form.FireShotFromConsole(x, y, usePlus: false, useXShape: false, useSuper: false);
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

            x = colChar - 'A'; // A -> 0, B -> 1...
            y = row - 1;       // 1 -> 0, 2 -> 1...

            return true;
        }
    }
}
