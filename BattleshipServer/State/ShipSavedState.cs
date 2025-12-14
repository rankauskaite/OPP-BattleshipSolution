using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    public sealed class ShipSavedState : ShipStateBase
    {
        public override string Name => "Išgelbėtas";

        public ShipSavedState(Ship ship) : base(ship)
        {
        }

        public override void Hit(CellState[,] board, int x, int y)
        {
            if (board[y, x] == CellState.Ship)
            {
                board[y, x] = CellState.Hit;
            }

            if (Ship.IsSunk(board))
            {
                Ship.MarkAsSunk(board);
                Ship.ChangeState(new ShipSunkState(Ship));
            }
            else
            {
                Ship.ChangeState(new ShipHitState(Ship));
            }
        }

        public override void Save(CellState[,] board)
        {
            throw new InvalidOperationException("Laivas jau yra išgelbėtas.");
        }
    }
}