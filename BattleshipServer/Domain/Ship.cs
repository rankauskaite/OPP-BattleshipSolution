using System;
using System.Collections.Generic;
using BattleshipServer.State;

namespace BattleshipServer.Domain
{
    /// <summary>
    /// Domeno lygio laivo modelis.
    /// Čia pritaikytas State pattern: kiekvienas laivas turi būseną
    /// (padėtas, pašautas, nušautas, išgelbėtas), kuri kinta priklausomai
    /// nuo šūvių į šio laivo langelius.
    /// </summary>
    public sealed class Ship
    {
        public int X { get; }
        public int Y { get; }
        public int Length { get; }
        public bool Horizontal { get; }
        public bool MarkedSunk { get; private set; }

        // --- State pattern dalis ---

        private IShipState _state;

        /// <summary>
        /// Dabartinė laivo būsena (padėtas / pašautas / nušautas / išgelbėtas).
        /// </summary>
        public IShipState State => _state;

        /// <summary>
        /// Patogus skaitymui būsenos pavadinimas (naudinga ataskaitoms / debug).
        /// </summary>
        public string StateName => _state.Name;

        public Ship(int x, int y, int length, bool horizontal)
        {
            X = x;
            Y = y;
            Length = length;
            Horizontal = horizontal;
            MarkedSunk = false;

            // Pradinė būsena – laivas tik padėtas į lentą.
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

        /// <summary>
        /// Pagal dabartinį lentos masyvą patikrina, ar VISI šio laivo langeliai
        /// yra arba Hit, arba Sunk. Ši logika naudojama tiek senuose metoduose,
        /// tiek naujuose state'uose.
        /// </summary>
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

        /// <summary>
        /// Pažymi visus laivo langelius kaip Sunk lentoje ir nustato MarkedSunk flag'ą.
        /// </summary>
        public void MarkAsSunk(CellState[,] board)
        {
            foreach (var (cx, cy) in Cells())
            {
                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10) continue;
                board[cy, cx] = CellState.Sunk;
            }
            MarkedSunk = true;
        }

        /// <summary>
        /// Iš išorės kviečiamas metodas, kai į šį laivą pataiko šūvis.
        /// Board masyvą ir konkrečią koordinatę perduodam būsenai, kuri nusprendžia,
        /// kaip keistis toliau (likti pašautam, pasikeisti į nuskendusį ir t.t.).
        /// </summary>
        public void RegisterHit(CellState[,] board, int x, int y)
        {
            _state.Hit(board, x, y);
        }

        /// <summary>
        /// Bandom išgelbėti laivą. Pagal užduotį – leidžiama tik tada,
        /// kai laivas yra būsenos „Pašautas“ (ShipHitState).
        /// </summary>
        public void TrySave(CellState[,] board)
        {
            _state.Save(board);
        }
    }
}