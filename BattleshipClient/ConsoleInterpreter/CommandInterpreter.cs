using System;

namespace BattleshipClient.ConsoleInterpreter
{
    public sealed class CommandInterpreter
    {
        public bool Interpret(string line, BattleshipClient.MainForm form)
        {
            var ctx = new ConsoleContext(line, form);
            var program = BuildAst(ctx);


            try { program.Execute(ctx); }
            catch (Exception ex) { ctx.Output($"[Interpreter] {ex.Message}"); }

            return ctx.ShouldExit;
        }

        private static ProgramNonTerminalExpression BuildAst(ConsoleContext ctx)
        {
            var program = new ProgramNonTerminalExpression();

            int i = 0;
            while (i < ctx.Tokens.Count)
            {
                var t = ctx.Tokens[i].ToLowerInvariant();

                if (t == ";") { i++; continue; }

                if (t is "help" or "?")
                {
                    program.Add(new HelpTerminalExpression());
                    i++;
                    continue;
                }

                if (t == "undo")
                {
                    program.Add(new UndoTerminalExpression());
                    i++;
                    continue;
                }

                if (t == "redo")
                {
                    program.Add(new RedoTerminalExpression());
                    i++;
                    continue;
                }

                if (t == "exit")
                {
                    program.Add(new ExitTerminalExpression());
                    i++;
                    continue;
                }

                if (t is "shoot" or "plus" or "x" or "super")
                {
                    if (i + 1 >= ctx.Tokens.Count)
                    {
                        program.Add(new UnknownTerminalExpression("Trūksta koordinačių (pvz. shoot B5)."));
                        break;
                    }

                    var verb = new VerbTerminalExpression(ctx.Tokens[i]);
                    var coord = new CoordinateTerminalExpression(ctx.Tokens[i + 1]);
                    program.Add(new ShotNonTerminalExpression(verb, coord));
                    i += 2;
                    continue;
                }

                program.Add(new UnknownTerminalExpression($"Nežinoma komanda/token: {ctx.Tokens[i]}"));
                i++;
            }

            return program;
        }
    }
}
