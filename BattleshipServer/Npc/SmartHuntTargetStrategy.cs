using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleshipServer.Npc
{
    public enum ShotOutcome { Miss, Hit }

    /// <summary>
    /// "Žmogiška" taktika:
    /// - Hunt (checkerboard): kol nėra hit'ų
    /// - Target: kai pataiko – tikrina N/E/S/W aplink
    /// - Line: jei 2+ hitai toje pačioje eilutėje/stulpelyje – šaudo tiesiai,
    ///         kol prameta; tada bando priešingą kryptį, ir tik tuomet grįžta į Hunt.
    /// </summary>
    public sealed class SmartHuntTargetStrategy
    {
        private readonly int _w, _h;
        private readonly HashSet<(int x,int y)> _shot = new();
        private readonly HashSet<(int x,int y)> _hits = new();

        private readonly Queue<(int x,int y)> _frontier = new();
        private (int x,int y)? _seedHit = null;         // pirmasis hit klasteryje
        private (int dx,int dy)? _lineDir = null;       // kryptis, kai žinome liniją
        private bool _reverseTried = false;

        public SmartHuntTargetStrategy(int width, int height)
        {
            _w = width; _h = height;
        }

        public (int x,int y) NextShot()
        {
            // 1) Linija – tęsti kryptimi
            if (_lineDir is { } dir && _seedHit.HasValue)
            {
                var seed = _seedHit.Value;
                var ordered = OrderedHitsAlongLine(seed, dir);
                var tail = ordered.Last();
                var nx = tail.x + dir.dx;
                var ny = tail.y + dir.dy;
                if (In(nx,ny) && !_shot.Contains((nx,ny))) return (nx,ny);

                // mėginame priešingą kryptį (vieną kartą)
                if (!_reverseTried)
                {
                    _reverseTried = true;
                    var rev = (-dir.dx, -dir.dy);
                    var head = ordered.First();
                    nx = head.x + rev.Item1; ny = head.y + rev.Item2;
                    if (In(nx,ny) && !_shot.Contains((nx,ny))) return (nx,ny);
                }
            }

            // 2) Target – kol turim "frontier"
            while (_frontier.Count > 0)
            {
                var c = _frontier.Dequeue();
                if (In(c.x,c.y) && !_shot.Contains(c)) return c;
            }

            // 3) Hunt – checkerboard
            var rnd = Random.Shared;
            for (int i = 0; i < 200; i++)
            {
                int x = rnd.Next(0,_w), y = rnd.Next(0,_h);
                if (((x+y)&1)==0 && !_shot.Contains((x,y))) return (x,y);
            }
            // fallback – bet kas, kas nešaudytas
            for (int i = 0; i < 400; i++)
            {
                int x = rnd.Next(0,_w), y = rnd.Next(0,_h);
                if (!_shot.Contains((x,y))) return (x,y);
            }
            return (0,0);
        }

        public void ObserveResult((int x,int y) cell, ShotOutcome outcome)
        {
            _shot.Add(cell);

            if (outcome == ShotOutcome.Miss)
            {
                // jei šovėm linija ir prametėm – NextShot() pabandys reverse (jei nebandėm)
                return;
            }

            // Hit
            _hits.Add(cell);

            if (_seedHit is null)
            {
                _seedHit = cell;
                EnqueueNeighbors(cell); // N,E,S,W
                return;
            }

            // Jei dar neturėjom krypties – bandome ją nustatyti
            if (_lineDir is null)
            {
                var dir = InferLineDirection(_seedHit.Value, cell);
                if (dir is { } d)
                {
                    _lineDir = d;
                    _reverseTried = false;
                }
                else
                {
                    // „L“ forma – pratęsiam kaimynais
                    EnqueueNeighbors(cell);
                }
            }
            // jei kryptis jau yra – tiesiog tęsime NextShot()
        }

        // ===== Helpers =====

        private bool In(int x,int y) => x>=0 && x<_w && y>=0 && y<_h;

        private void EnqueueNeighbors((int x,int y) c)
        {
            var around = new [] { (c.x, c.y-1), (c.x+1, c.y), (c.x, c.y+1), (c.x-1, c.y) }; // N,E,S,W
            foreach (var p in around)
                if (In(p.Item1,p.Item2) && !_shot.Contains(p) && !_frontier.Contains(p))
                    _frontier.Enqueue(p);
        }

        private (int dx,int dy)? InferLineDirection((int x,int y) a, (int x,int y) b)
        {
            if (a.x == b.x) return (0,1);   // vertikali
            if (a.y == b.y) return (1,0);   // horizontali
            return null;
        }

        private IEnumerable<(int x,int y)> OrderedHitsAlongLine((int x,int y) seed, (int dx,int dy) dir)
        {
            var line = _hits
                .Where(h => (dir.dx==0 ? h.x==seed.x : h.y==seed.y))
                .OrderBy(h => dir.dx!=0 ? h.x : h.y)
                .ToList();

            if (line.Count == 0) line.Add(seed);
            return line;
        }
    }
}
