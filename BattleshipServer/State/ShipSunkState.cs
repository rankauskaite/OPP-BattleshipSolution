using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    /// <summary>
    /// Laivas yra nuskandintas (nušautas).
    /// </summary>
    public sealed class ShipSunkState : ShipStateBase
    {
        public override string Name => "Nušautas";

        public ShipSunkState(Ship ship) : base(ship)
        {
        }

        public override void Enter()
        {
            // Čia galėtum informuoti žaidimą / statistikas, kad laivas nuskendo.
            // Šiuo metu Domain sluoksnyje užtenka paties ženklo lentoje (MarkAsSunk).
        }

        public override void Hit(CellState[,] board, int x, int y)
        {
            // Šūvis į jau nuskandintą laivą – traktuojame kaip negalimą (arba tiesiog ignoruojam).
            throw new InvalidOperationException("Laivas jau nušautas – papildomi šūviai nereikalingi.");
        }

        public override void Save(CellState[,] board)
        {
            // Pagal užduotį – išgelbėti galima tik pašautą, bet dar nenušautą.
            throw new InvalidOperationException("Negalima išgelbėti jau nušauto laivo.");
        }
    }
}