using System;
using System.Collections.Generic;
using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    [ShotStrategy("human-like")]
    public sealed class HumanLikeFrontierHeatStrategy : INpcShotStrategy
    {
        private static readonly int[] DefaultFleet = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            var unknown = k.UnshotCells().Select(c => (x: c.X, y: c.Y)).ToList();
            if (unknown.Count == 0) return (0, 0);

            // TARGET: šalia pataikymų
            var frontier = k.HitFrontier4().Select(c => (x: c.X, y: c.Y)).Distinct().ToList();
            if (frontier.Count > 0)
            {
                var set = new HashSet<(int x, int y)>(frontier);

                // „Skylė“ H ? H
                foreach (var p in frontier)
                {
                    bool hGap = set.Contains((p.x - 1, p.y)) && set.Contains((p.x + 1, p.y));
                    bool vGap = set.Contains((p.x, p.y - 1)) && set.Contains((p.x, p.y + 1));
                    if (hGap || vGap) return p;
                }

                // Linijos tęstinumas
                int Score((int x, int y) q)
                {
                    int s = 0;
                    if (set.Contains((q.x - 1, q.y))) s++;
                    if (set.Contains((q.x + 1, q.y))) s++;
                    if (set.Contains((q.x, q.y - 1))) s++;
                    if (set.Contains((q.x, q.y + 1))) s++;
                    if (set.Contains((q.x - 1, q.y)) && set.Contains((q.x + 1, q.y))) s += 2;
                    if (set.Contains((q.x, q.y - 1)) && set.Contains((q.x, q.y + 1))) s += 2;
                    return s;
                }

                return frontier
                    .OrderByDescending(Score)
                    .ThenBy(_ => Random.Shared.Next())
                    .First();
            }

            // HUNT: paritetas
            int w = unknown.Max(p => p.x) + 1;
            int h = unknown.Max(p => p.y) + 1;
            int parity = 0;

            var parityCand = unknown.Where(p => (((p.x + p.y) & 1) == parity)).ToList();
            if (parityCand.Count > 0)
            {
                double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0;
                int ScoreCenter((int x, int y) p)
                {
                    double dx = p.x - cx, dy = p.y - cy;
                    double r = Math.Min(w, h) / 3.0;
                    return (dx * dx + dy * dy) <= r * r ? 1 : 0;
                }

                return parityCand
                    .OrderByDescending(ScoreCenter)
                    .ThenBy(_ => Random.Shared.Next())
                    .First();
            }

            // HUNT: mini heatmap (tilpimų skaičius)
            var unknownSet = new HashSet<(int x, int y)>(unknown);
            var score = new Dictionary<(int x, int y), int>();
            void Add((int x, int y) p, int v) { if (!score.ContainsKey(p)) score[p] = 0; score[p] += v; }

            foreach (var size in DefaultFleet)
            {
                // Horizontalūs
                for (int y = 0; y < h; y++)
                for (int x0 = 0; x0 <= w - size; x0++)
                {
                    bool ok = true;
                    for (int dx = 0; dx < size; dx++)
                        if (!unknownSet.Contains((x0 + dx, y))) { ok = false; break; }
                    if (!ok) continue;
                    for (int dx = 0; dx < size; dx++) Add((x0 + dx, y), 1);
                }

                // Vertikalūs
                for (int x = 0; x < w; x++)
                for (int y0 = 0; y0 <= h - size; y0++)
                {
                    bool ok = true;
                    for (int dy = 0; dy < size; dy++)
                        if (!unknownSet.Contains((x, y0 + dy))) { ok = false; break; }
                    if (!ok) continue;
                    for (int dy = 0; dy < size; dy++) Add((x, y0 + dy), 1);
                }
            }

            if (score.Count > 0)
            {
                int max = score.Max(kv => kv.Value);
                var best = score.Where(kv => kv.Value == max).Select(kv => kv.Key).ToList();
                return best[Random.Shared.Next(best.Count)];
            }

            var rnd = unknown[Random.Shared.Next(unknown.Count)];
            return (rnd.x, rnd.y);
        }
    }
}
