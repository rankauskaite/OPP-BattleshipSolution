using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    /// <summary>
    /// Laivas yra padėtas lentoje, dar nepatyręs jokio pataikymo.
    /// </summary>
    public sealed class ShipPlacedState : ShipStateBase
    {
        public override string Name => "Padėtas";

        public ShipPlacedState(Ship ship) : base(ship)
        {
        }

        public override void Hit(CellState[,] board, int x, int y)
        {
            // Pažymime pataikytą langelį lentoje (jei dar nepažymėtas).
            if (board[y, x] == CellState.Ship)
            {
                board[y, x] = CellState.Hit;
            }

            // Patikriname, ar po šio šūvio laivas jau nuskendo.
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
            // Pagal užduotį – išgelbėti galima tik pašautą, bet nenušautą laivą.
            throw new InvalidOperationException("Negalima išgelbėti laivo, kuris dar nėra pašautas.");
        }
    }
}