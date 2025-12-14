using System;

namespace BattleshipClient.ConsoleInterpreter
{
    // NonTerminal: programa = daug komandų
    public sealed class ProgramNonTerminalExpression : NonTerminalExpression
    {
        public void Add(IExpression expr) => AddToList(expr);

        public override void Execute(ConsoleContext ctx)
        {
            foreach (var child in list)
            {
                try
                {
                    child.Execute(ctx);
                }
                catch (Exception ex)
                {
                    ctx.Output($"[Interpreter] {ex.Message}");
                }
                if (ctx.ShouldExit) return;
            }
        }
    }

    // NonTerminal: šūvis = (veiksmas + koordinatės)
    public sealed class ShotNonTerminalExpression : NonTerminalExpression
    {
        private readonly IExpression _verb;
        private readonly IExpression _coord;

        public ShotNonTerminalExpression(IExpression verb, IExpression coord)
        {
            _verb = verb ?? throw new ArgumentNullException(nameof(verb));
            _coord = coord ?? throw new ArgumentNullException(nameof(coord));

            // laikom kaip AST vaikus (kad UML sutaptų su skaidrėmis)
            AddToList(_verb);
            AddToList(_coord);
        }

        public override void Execute(ConsoleContext ctx)
        {
            _verb.Execute(ctx);
            _coord.Execute(ctx);

            // vykdymas UI gijoje
            ctx.Ui(() => ctx.Form.FireShotFromConsole(ctx.X, ctx.Y, ctx.UsePlus, ctx.UseX, ctx.UseSuper));
        }
    }
}
