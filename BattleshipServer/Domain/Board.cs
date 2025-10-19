using System.Collections.Generic;
using System.Linq;
using BattleshipServer.Models;
using BattleshipServer.Domain;

namespace BattleshipServer.Domain
{
    public enum ShotKind { Miss, Hit, Sunk }

    public sealed class ShotOutcome
    {
        public ShotKind Kind { get; init; }
        public Coordinate Cell { get; init; }
        public List<Coordinate>? SunkCells { get; init; } // kai Sunk
    }

    public sealed class Board
    {
        public const int Size = 10;

        public CellState[,] Cells { get; } = new CellState[Size, Size];
        public List<Ship> Ships { get; } = new();

        public void Reset()
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    Cells[y, x] = CellState.Empty;
            Ships.Clear();
        }

        public void PlaceShips(IEnumerable<ShipDto> ships)
        {
            Reset();
            foreach (var s in ships)
            {
                var ship = new Ship(s.X, s.Y, s.Len, (s.Dir ?? "H").ToUpper() == "H");
                Ships.Add(ship);
                foreach (var (x, y) in ship.Cells())
                    if (x >= 0 && x < Size && y >= 0 && y < Size)
                        Cells[y, x] = CellState.Ship;
            }
        }

        public bool AllShipsSunk()
        {
            return Ships.All(s => s.IsSunk(Cells) || s.MarkedSunk);
        }

        /// <summary>
        /// Pritaiko šūvį. Grąžina rezultatą ir, jeigu nuskendo, visų nuskendusio laivo langelių sąrašą.
        /// </summary>
        public ShotOutcome ApplyShot(int x, int y)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size)
                return new ShotOutcome { Kind = ShotKind.Miss, Cell = new Coordinate(x, y) }; // saugiklis

            var cell = Cells[y, x];
            if (cell == CellState.Empty)
            {
                Cells[y, x] = CellState.Miss;
                return new ShotOutcome { Kind = ShotKind.Miss, Cell = new Coordinate(x, y) };
            }
            if (cell == CellState.Ship)
            {
                Cells[y, x] = CellState.Hit;

                // ar nuskendo konkretus laivas?
                var victim = Ships.FirstOrDefault(s => s.Contains(x, y));
                if (victim != null && victim.IsSunk(Cells) && !victim.MarkedSunk)
                {
                    victim.MarkAsSunk(Cells);
                    return new ShotOutcome
                    {
                        Kind = ShotKind.Sunk,
                        Cell = new Coordinate(x, y),
                        SunkCells = victim.Cells().ToList()
                    };
                }

                return new ShotOutcome { Kind = ShotKind.Hit, Cell = new Coordinate(x, y) };
            }

            // buvo Hit/Miss/Sunk – pakartotas šūvis: traktuojam kaip Miss (neperjungiant ėjimo tvarkai sprendžia Game)
            return new ShotOutcome { Kind = ShotKind.Miss, Cell = new Coordinate(x, y) };
        }
    }
}
