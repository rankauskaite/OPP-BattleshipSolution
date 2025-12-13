using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    /// <summary>
    /// Laivas bent kartą pataikytas, bet dar visiškai nenušautas.
    /// Būtent iš šios būsenos galima jį „išgelbėti“.
    /// </summary>
    public sealed class ShipHitState : ShipStateBase
    {
        public override string Name => "Pašautas";

        public ShipHitState(Ship ship) : base(ship)
        {
        }

        public override void Hit(CellState[,] board, int x, int y)
        {
            // Jei šūvis pataiko į dar nepažeistą šio laivo langelį – pažymime jį kaip Hit.
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
                // Laivas toliau lieka „pašautas“ (gali būti keli hit'ai)
                Ship.ChangeState(new ShipHitState(Ship));
            }
        }

        public override void Save(CellState[,] board)
        {
            // Čia įgyvendinam reikalavimą: išgelbėti galima tik pašautą, bet nenušautą.
            // KĄ reiškia „išgelbėti“? Paprastas pasirinkimas – atstatyti visus Hit langelius atgal į Ship.

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