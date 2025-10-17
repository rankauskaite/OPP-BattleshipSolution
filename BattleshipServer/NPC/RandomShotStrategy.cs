using System;
using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    //[ShotStrategy("random")] 
    public sealed class RandomShotStrategy : INpcShotStrategy
    {
        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            var cells = k.UnshotCells().Select(c => (x: c.X, y: c.Y)).ToList();
            if (cells.Count == 0) return (0, 0);
            var p = cells[Random.Shared.Next(cells.Count)];
            return (p.x, p.y);
        }
    }
}
