using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    public interface IShipState
    {
        Ship Ship { get; }
        string Name { get; }

        void Enter();
        void Hit(CellState[,] board, int x, int y);
        void Save(CellState[,] board);
    }
}