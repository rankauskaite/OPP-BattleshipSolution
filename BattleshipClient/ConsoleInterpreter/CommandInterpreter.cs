using System;

namespace BattleshipClient.ConsoleInterpreter
{
    public sealed class CommandInterpreter
    {
        /// <summary>
        /// Interpretuoja vieną įvesties eilutę.
        /// Grąžina true, jei interpretavimo metu buvo paprašyta išeiti (exit).
        /// </summary>
        public bool Interpret(string line, BattleshipClient.MainForm form)
        {
            var ctx = new ConsoleContext(line, form);
            var program = BuildAst(ctx);

            try
            {
                program.Interpret(ctx);
            }
            catch (Exception ex)
            {
                ctx.Output($"[Interpreter] {ex.Message}");
            }

            return ctx.ShouldExit;
        }

        private static ProgramNonTerminalExpression BuildAst(ConsoleContext ctx)
        {
            var program = new ProgramNonTerminalExpression();

            static bool IsCommandWord(string token)
            {
                token = token.ToLowerInvariant();
                return token is ";" or "help" or "?" or "undo" or "redo" or "exit" or "shoot" or "plus" or "x" or "super";
            }

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

                // Grammar: <verb> <coord>
                if (t is "shoot" or "plus" or "x" or "super")
                {
                    if (i + 1 >= ctx.Tokens.Count)
                    {
                        program.Add(new UnknownTerminalExpression("Trūksta koordinačių (pvz. shoot B5)."));
                        i++;
                        continue;
                    }

                    // Pvz. "shoot ; undo" arba "shoot exit" – laikom, kad trūksta koordinačių,
                    // bet tęsiam interpretavimą toliau.
                    var next = ctx.Tokens[i + 1];
                    if (IsCommandWord(next))
                    {
                        program.Add(new UnknownTerminalExpression("Trūksta koordinačių (pvz. shoot B5)."));
                        i++;
                        continue;
                    }

                    var verb = new VerbTerminalExpression(ctx.Tokens[i]);
                    var coord = new CoordinateTerminalExpression(next);
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
