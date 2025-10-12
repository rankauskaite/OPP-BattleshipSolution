using System;
using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    [ShotStrategy("checkerboard")]
    public sealed class CheckerboardStrategy : INpcShotStrategy
    {
        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            var unknown = k.UnshotCells().Select(c => (x: c.X, y: c.Y)).ToList();
            if (unknown.Count == 0) return (0, 0);

            var cb = unknown.Where(p => (((p.x + p.y) & 1) == 0)).ToList();
            var list = cb.Count > 0 ? cb : unknown;

            var pick = list[Random.Shared.Next(list.Count)];
            return (pick.x, pick.y);
        }
    }
}
