using System;
using System.Collections.Generic;

namespace BattleshipClient.ConsoleInterpreter
{
    // NonTerminal: programa = daug komandų
    public sealed class ProgramNonTerminalExpression : IExpression
    {
        private readonly List<IExpression> _children = new();

        public void Add(IExpression expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));
            _children.Add(expr);
        }

        public void Interpret(ConsoleContext ctx)
        {
            foreach (var child in _children)
            {
                try
                {
                    child.Interpret(ctx);
                }
                catch (Exception ex)
                {
                    // Nesustabdom visos programos dėl vienos blogos komandos – tai patogiau konsolėje.
                    ctx.Output($"[Interpreter] {ex.Message}");
                }

                if (ctx.ShouldExit) return;
            }
        }
    }

    // NonTerminal: šūvis = (veiksmas + koordinatės)
    public sealed class ShotNonTerminalExpression : IExpression
    {
        private readonly IExpression _verb;
        private readonly IExpression _coord;

        public ShotNonTerminalExpression(IExpression verb, IExpression coord)
        {
            _verb = verb ?? throw new ArgumentNullException(nameof(verb));
            _coord = coord ?? throw new ArgumentNullException(nameof(coord));
        }

        public void Interpret(ConsoleContext ctx)
        {
            _verb.Interpret(ctx);
            _coord.Interpret(ctx);

            // vykdymas UI gijoje
            ctx.Ui(() => ctx.Form.FireShotFromConsole(ctx.X, ctx.Y, ctx.UsePlus, ctx.UseX, ctx.UseSuper));
        }
    }
}
