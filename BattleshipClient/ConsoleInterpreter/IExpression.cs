using System;

namespace BattleshipClient.ConsoleInterpreter
{
    public interface IExpression
    {
        bool CanInterpret(ConsoleContext context);
        void Interpret(ConsoleContext context);
    }
}
