using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    public sealed class ShipPlacedState : ShipStateBase
    {
        public override string Name => "Padėtas";

        public ShipPlacedState(Ship ship) : base(ship)
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
            throw new InvalidOperationException("Negalima išgelbėti laivo, kuris dar nėra pašautas.");
        }
    }
}