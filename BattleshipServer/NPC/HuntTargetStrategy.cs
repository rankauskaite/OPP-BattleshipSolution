using System;
using System.Collections.Generic;
using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    //[ShotStrategy("hunt-target")]
    public sealed class HuntTargetStrategy : INpcShotStrategy
    {
        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            var unknown = k.UnshotCells().Select(c => (x: c.X, y: c.Y)).ToList();
            if (unknown.Count == 0) return (0, 0);

            var frontier = k.HitFrontier4().Select(c => (x: c.X, y: c.Y)).Distinct().ToList();
            if (frontier.Count > 0)
            {
                var set = new HashSet<(int x, int y)>(frontier);

                // paprastas reitingas pagal kaimynus
                int Score((int x, int y) p)
                {
                    int s = 0;
                    if (set.Contains((p.x - 1, p.y))) s++;
                    if (set.Contains((p.x + 1, p.y))) s++;
                    if (set.Contains((p.x, p.y - 1))) s++;
                    if (set.Contains((p.x, p.y + 1))) s++;
                    return s;
                }

                return frontier
                    .OrderByDescending(Score)
                    .ThenBy(_ => Random.Shared.Next())
                    .First();
            }

            // fallback į checkerboard
            var cb = unknown.Where(p => (((p.x + p.y) & 1) == 0)).ToList();
            var list = cb.Count > 0 ? cb : unknown;
            var pick = list[Random.Shared.Next(list.Count)];
            return (pick.x, pick.y);
        }
    }
}
