using System;
using System.Collections.Generic;
using BattleshipServer.State;

namespace BattleshipServer.Domain
{
    public sealed class Ship
    {
        public int X { get; }
        public int Y { get; }
        public int Length { get; }
        public bool Horizontal { get; }
        public bool MarkedSunk { get; private set; }

        private IShipState _state;
        public IShipState State => _state;
        public string StateName => _state.Name;

        public Ship(int x, int y, int length, bool horizontal)
        {
            X = x;
            Y = y;
            Length = length;
            Horizontal = horizontal;
            MarkedSunk = false;

            ChangeState(new ShipPlacedState(this));
        }

        internal void ChangeState(IShipState newState)
        {
            _state = newState ?? throw new ArgumentNullException(nameof(newState));
            _state.Enter();
        }

        public IEnumerable<Coordinate> Cells()
        {
            for (int i = 0; i < Length; i++)
                yield return new Coordinate(
                    X + (Horizontal ? i : 0),
                    Y + (Horizontal ? 0 : i));
        }

        public bool Contains(int x, int y)
        {
            if (Horizontal) return y == Y && x >= X && x < X + Length;
            return x == X && y >= Y && y < Y + Length;
        }

        public bool IsSunk(CellState[,] board)
        {
            foreach (var (cx, cy) in Cells())
            {
                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10) return false;
                var cell = board[cy, cx];
                if (cell != CellState.Hit && cell != CellState.Sunk) return false;
            }
            return true;
        }

        public void MarkAsSunk(CellState[,] board)
        {
            foreach (var (cx, cy) in Cells())
            {
                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10) continue;
                board[cy, cx] = CellState.Sunk;
            }
            MarkedSunk = true;
        }

        public void RegisterHit(CellState[,] board, int x, int y)
        {
            _state.Hit(board, x, y);
        }

        public void TrySave(CellState[,] board)
        {
            _state.Save(board);
        }
    }
}