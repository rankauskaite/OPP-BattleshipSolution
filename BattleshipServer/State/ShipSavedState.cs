using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    /// <summary>
    /// Laivas buvo pašautas, bet buvo „išgelbėtas“ (pvz., specialaus power-up pagalba).
    /// Šiuo metu tai reiškia, kad visi jo Hit langeliai grąžinti į Ship būseną.
    /// </summary>
    public sealed class ShipSavedState : ShipStateBase
    {
        public override string Name => "Išgelbėtas";

        public ShipSavedState(Ship ship) : base(ship)
        {
        }

        public override void Hit(CellState[,] board, int x, int y)
        {
            // Patekęs šūvis į išgelbėtą laivą vėl jį pažeidžia.
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
            // Paprastesnis ir aiškus variantas – neleisti iš naujo gelbėti jau išgelbėto laivo.
            throw new InvalidOperationException("Laivas jau yra išgelbėtas.");
        }
    }
}