using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    public sealed class ShipSunkState : ShipStateBase
    {
        public override string Name => "Nušautas";

        public ShipSunkState(Ship ship) : base(ship)
        {
        }

        public override void Enter()
        {
        }

        public override void Hit(CellState[,] board, int x, int y)
        {
            throw new InvalidOperationException("Laivas jau nušautas – papildomi šūviai nereikalingi.");
        }

        public override void Save(CellState[,] board)
        {
            throw new InvalidOperationException("Negalima išgelbėti jau nušauto laivo.");
        }
    }
}