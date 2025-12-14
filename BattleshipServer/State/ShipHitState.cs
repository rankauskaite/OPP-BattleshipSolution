using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    public sealed class ShipHitState : ShipStateBase
    {
        public override string Name => "Pašautas";

        public ShipHitState(Ship ship) : base(ship)
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
            foreach (var (cx, cy) in Ship.Cells())
            {
                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10) continue;
                if (board[cy, cx] == CellState.Hit)
                {
                    board[cy, cx] = CellState.Ship;
                }
            }

            Ship.ChangeState(new ShipSavedState(Ship));
        }
    }
}