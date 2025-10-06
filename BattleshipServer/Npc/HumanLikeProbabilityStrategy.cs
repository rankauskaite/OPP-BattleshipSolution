using System;
using System.Collections.Generic;
using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    /// <summary>
    /// „Žmogiška“ NPC strategija, suderinta su esamu BoardKnowledge API.
    /// Naudoja tik: HitFrontier4() ir UnshotCells().
    ///
    /// Logika:
    /// 1) TARGET: jei yra frontier (nežinomi langeliai aplink pataikymus),
    ///    - pirma „vieno langelio skylės“ (H ? H arba vertikalus analogas – aproksimuota per frontier kaimynystę),
    ///    - tada tęsiama tikėtina kryptis (pagal frontier kaimynų skaičių N/E/S/W).
    /// 2) HUNT: checkerboard (paritetas). Jei paritetas išsisemia – lengvas heatmap’as be pilno grid’o.
    /// </summary>
    public sealed class HumanLikeFrontierHeatStrategy : INpcShotStrategy
    {
        // Jei neturi dinaminės info apie likusius laivus – laikom klasiką
        private static readonly int[] DefaultFleet = new[] { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            // Visus nešautus laikome kaip „unknown“ taškus
            var unknown = k.UnshotCells().Select(c => (x: c.X, y: c.Y)).ToList();
            if (unknown.Count == 0) return (0, 0);

            // Apytikris lentos dydis (iš unknown ribų)
            int w = unknown.Max(p => p.x) + 1;
            int h = unknown.Max(p => p.y) + 1;

            // 1) TARGET (frontier aplink pataikymus)
            var frontier = k.HitFrontier4().Select(c => (x: c.X, y: c.Y)).Distinct().ToList();
            if (frontier.Count > 0)
            {
                var fset = new HashSet<(int x, int y)>(frontier);

                // 1.1 „Vieno langelio skylės“: jei taška turintis abu priešpriešinius frontier kaimynus,
                // tai greičiausiai H ? H situacija -> šaudom ten pirmiau.
                foreach (var p in frontier)
                {
                    bool hGap = fset.Contains((p.x - 1, p.y)) && fset.Contains((p.x + 1, p.y));
                    bool vGap = fset.Contains((p.x, p.y - 1)) && fset.Contains((p.x, p.y + 1));
                    if (hGap || vGap) return p;
                }

                // 1.2 Linijos tęstinumas: prioritetas taškams, kurie turi daugiau frontier kaimynų N/E/S/W,
                // bei bonusas už „poras“ (kairė+dešinė / viršus+apačia).
                int Score((int x, int y) q)
                {
                    int s = 0;
                    if (fset.Contains((q.x - 1, q.y))) s++;
                    if (fset.Contains((q.x + 1, q.y))) s++;
                    if (fset.Contains((q.x, q.y - 1))) s++;
                    if (fset.Contains((q.x, q.y + 1))) s++;
                    if (fset.Contains((q.x - 1, q.y)) && fset.Contains((q.x + 1, q.y))) s += 2;
                    if (fset.Contains((q.x, q.y - 1)) && fset.Contains((q.x, q.y + 1))) s += 2;
                    return s;
                }

                var bestFrontier = frontier
                    .OrderByDescending(Score)
                    .ThenBy(_ => Random.Shared.Next())
                    .First();

                return bestFrontier;
            }

            // 2) HUNT: checkerboard (paritetas pagal ilgiausią laivą)
            int maxShip = DefaultFleet.Max();
            bool useParity = maxShip >= 2;
            int parity = 0; // (x + y) % 2

            var parityCand = unknown.Where(p => !useParity || (((p.x + p.y) & 1) == parity)).ToList();
            if (parityCand.Count > 0)
            {
                // Lengvas centro bias (žmogiška intuicija)
                double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0;
                int ScoreCenter((int x, int y) p)
                {
                    double dist = Math.Sqrt((p.x - cx) * (p.x - cx) + (p.y - cy) * (p.y - cy));
                    return dist <= Math.Min(w, h) / 3.0 ? 1 : 0;
                }

                var best = parityCand
                    .OrderByDescending(ScoreCenter)
                    .ThenBy(_ => Random.Shared.Next())
                    .First();
                return (best.x, best.y);
            }

            // 3) Jei paritetas išsisėmė, darom lengvą heatmap be pilno grid’o:
            // - kiekvienam galimam laivo „įstatymui“ pridedam tašką pozicijoms, kurios dar yra unknown.
            var unknownSet = new HashSet<(int x, int y)>(unknown);
            var score = new Dictionary<(int x, int y), int>();
            void AddScore((int x, int y) p, int v)
            {
                if (!score.ContainsKey(p)) score[p] = 0;
                score[p] += v;
            }

            foreach (var size in DefaultFleet)
            {
                // Horizontalūs įstatymai
                for (int y = 0; y < h; y++)
                for (int x0 = 0; x0 <= w - size; x0++)
                {
                    bool allUnknown = true;
                    for (int dx = 0; dx < size; dx++)
                    {
                        if (!unknownSet.Contains((x0 + dx, y))) { allUnknown = false; break; }
                    }
                    if (!allUnknown) continue;

                    for (int dx = 0; dx < size; dx++) AddScore((x0 + dx, y), 1);
                }

                // Vertikalūs įstatymai
                for (int x = 0; x < w; x++)
                for (int y0 = 0; y0 <= h - size; y0++)
                {
                    bool allUnknown = true;
                    for (int dy = 0; dy < size; dy++)
                    {
                        if (!unknownSet.Contains((x, y0 + dy))) { allUnknown = false; break; }
                    }
                    if (!allUnknown) continue;

                    for (int dy = 0; dy < size; dy++) AddScore((x, y0 + dy), 1);
                }
            }

            if (score.Count > 0)
            {
                int max = score.Max(kv => kv.Value);
                var best = score.Where(kv => kv.Value == max).Select(kv => kv.Key).ToList();
                var pick = best[Random.Shared.Next(best.Count)];
                return pick;
            }

            // 4) Fallback – bet kuris unknown
            var rnd = unknown[Random.Shared.Next(unknown.Count)];
            return (rnd.x, rnd.y);
        }
    }
}
