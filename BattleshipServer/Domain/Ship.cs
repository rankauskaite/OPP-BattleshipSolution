using System.Collections.Generic;

namespace BattleshipServer.Domain
{
    public sealed class Ship
    {
        public int X { get; }
        public int Y { get; }
        public int Length { get; }
        public bool Horizontal { get; }
        public bool MarkedSunk { get; private set; }

        public Ship(int x, int y, int length, bool horizontal)
        {
            X = x; Y = y; Length = length; Horizontal = horizontal;
            MarkedSunk = false;
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
    }
}
