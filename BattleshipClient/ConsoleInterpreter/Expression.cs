using System;
using System.Collections.Generic;

namespace BattleshipClient.ConsoleInterpreter
{

    public interface IExpression
    {
        void Execute(ConsoleContext ctx);
    }


    public abstract class TerminalExpression : IExpression
    {
        public abstract void Execute(ConsoleContext ctx);
    }


    public abstract class NonTerminalExpression : IExpression
    {
        protected readonly List<IExpression> list = new();

        protected void AddToList(IExpression expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));
            list.Add(expr);
        }

        public abstract void Execute(ConsoleContext ctx);
    }
}
