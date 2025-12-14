using System;

namespace BattleshipClient.ConsoleInterpreter
{
    // Terminal: veiksmo žodis (shoot/plus/x/super)
    public sealed class VerbTerminalExpression : IExpression
    {
        private readonly string _verb;
        public VerbTerminalExpression(string verb) => _verb = verb ?? "";

        public void Interpret(ConsoleContext ctx)
        {
            var v = _verb.Trim().ToLowerInvariant();

            ctx.UsePlus = ctx.UseX = ctx.UseSuper = false;

            switch (v)
            {
                case "shoot": break;
                case "plus":  ctx.UsePlus = true; break;
                case "x":     ctx.UseX = true; break;
                case "super": ctx.UseSuper = true; break;
                default: throw new ArgumentException($"Nežinomas veiksmas: {v}");
            }
        }
    }

    // Terminal: koordinatės (B5, J10...)
    public sealed class CoordinateTerminalExpression : IExpression
    {
        private readonly string _token;
        public CoordinateTerminalExpression(string token) => _token = token ?? "";

        public void Interpret(ConsoleContext ctx)
        {
            if (!TryParse(_token, out var x, out var y))
                throw new ArgumentException($"Blogos koordinatės: {_token} (pvz. B5, J10)");

            ctx.X = x;
            ctx.Y = y;
        }

        public static bool TryParse(string token, out int x, out int y)
        {
            x = -1; y = -1;
            if (string.IsNullOrWhiteSpace(token) || token.Length < 2) return false;

            token = token.Trim().ToUpperInvariant();
            char col = token[0];
            if (col < 'A' || col > 'J') return false;

            if (!int.TryParse(token.Substring(1), out int row)) return false;
            if (row < 1 || row > 10) return false;

            x = col - 'A';
            y = row - 1;
            return true;
        }
    }

    public sealed class HelpTerminalExpression : IExpression
    {
        public void Interpret(ConsoleContext ctx)
        {
            ctx.Output("Komandos:");
            ctx.Output("  shoot B5");
            ctx.Output("  plus  C7");
            ctx.Output("  x     D4");
            ctx.Output("  super A1");
            ctx.Output("  undo");
            ctx.Output("  redo");
            ctx.Output("  help / ?");
            ctx.Output("  exit");
        }
    }

    public sealed class UndoTerminalExpression : IExpression
    {
        public void Interpret(ConsoleContext ctx)
        {
            ctx.Ui(() =>
            {
                if (ctx.Form.CommandManager.CanUndo)
                    ctx.Form.CommandManager.Undo();
            });
        }
    }

    public sealed class RedoTerminalExpression : IExpression
    {
        public void Interpret(ConsoleContext ctx)
        {
            ctx.Ui(() =>
            {
                if (ctx.Form.CommandManager.CanRedo)
                    ctx.Form.CommandManager.Redo();
            });
        }
    }

    public sealed class ExitTerminalExpression : IExpression
    {
        public void Interpret(ConsoleContext ctx) => ctx.ShouldExit = true;
    }

    public sealed class UnknownTerminalExpression : IExpression
    {
        private readonly string _msg;
        public UnknownTerminalExpression(string msg) => _msg = msg;
        public void Interpret(ConsoleContext ctx) => ctx.Output($"[Interpreter] {_msg}");
    }
}
