using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public sealed class HuntTargetStrategy : INpcShotStrategy
    {
        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            var f = k.HitFrontier4().ToList();
            if (f.Count > 0)
            {
                var pick = f[Random.Shared.Next(f.Count)];
                return (pick.X, pick.Y);
            }
            // fallback – checkerboard
            var cand = k.UnshotCells().Where(c => ((c.X + c.Y) & 1) == 0).ToList();
            if (cand.Count == 0) cand = k.UnshotCells().ToList();
            var p = cand[Random.Shared.Next(cand.Count)];
            return (p.X, p.Y);
        }
    }
}
