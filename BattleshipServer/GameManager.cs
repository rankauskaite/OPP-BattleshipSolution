using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipServer.Models;
using BattleshipServer.Data;

namespace BattleshipServer
{
    public class GameManager
    {
        private readonly ConcurrentQueue<PlayerConnection> _waiting = new();
        private readonly ConcurrentDictionary<Guid, Game> _playerToGame = new();
        private readonly List<Game> _games = new();
        private readonly Database _db = new Database("battleship.db");

        // Bot jungtis per žaidimą
        private readonly ConcurrentDictionary<Game, PlayerConnection> _gameBots = new();

        // Bot strategija per žaidimą
        private readonly ConcurrentDictionary<Game, SmartHuntTargetStrategy> _smartByGame = new();

        public async Task HandleMessageAsync(PlayerConnection player, MessageDto dto)
        {
            switch (dto.Type)
            {
                case "register":
                {
                    if (dto.Payload.TryGetProperty("playerName", out var nmElem))
                        player.Name = nmElem.GetString();

                    _waiting.Enqueue(player);
                    Console.WriteLine($"[Manager] Player registered: {player.Name} ({player.Id})");
                    await player.SendAsync(new MessageDto
                    {
                        Type = "register",
                        Payload = JsonDocument.Parse("{\"message\":\"registered\"}").RootElement
                    });
                    TryPairPlayers();
                    break;
                }

                case "playWithBot":
                {
                    if (dto.Payload.TryGetProperty("playerName", out var nmElem))
                        player.Name = nmElem.GetString();

                    await CreateBotGameAsync(player);
                    break;
                }

                case "ready":
                {
                    if (_playerToGame.TryGetValue(player.Id, out var gReady))
                    {
                        var ships = new List<ShipDto>();
                        if (dto.Payload.TryGetProperty("ships", out var shipsElem))
                        {
                            foreach (var el in shipsElem.EnumerateArray())
                            {
                                ships.Add(new ShipDto
                                {
                                    X   = el.GetProperty("x").GetInt32(),
                                    Y   = el.GetProperty("y").GetInt32(),
                                    Len = el.GetProperty("len").GetInt32(),
                                    Dir = el.GetProperty("dir").GetString()
                                });
                            }
                        }

                        gReady.PlaceShips(player.Id, ships);
                        Console.WriteLine($"[Manager] Player {player.Name} placed {ships.Count} ships.");

                        if (gReady.IsReady)
                        {
                            await gReady.StartGame();

                            if (_gameBots.TryGetValue(gReady, out var bot) && gReady.CurrentPlayerId == bot.Id)
                                await TryBotChainAsync(gReady, bot);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Manager] Ready received but player not in a game yet.");
                    }
                    break;
                }

                case "shot":
                {
                    if (dto.Payload.TryGetProperty("x", out var xe) && dto.Payload.TryGetProperty("y", out var ye))
                    {
                        int x = xe.GetInt32();
                        int y = ye.GetInt32();
                        if (_playerToGame.TryGetValue(player.Id, out var gShot))
                        {
                            await gShot.ProcessShot(player.Id, x, y); 
                            
                            if (gShot.IsOver) return;


                            if (_gameBots.TryGetValue(gShot, out var bot) && gShot.CurrentPlayerId == bot.Id)
                                    await TryBotChainAsync(gShot, bot);
                        }
                    }
                    break;
                }

                default:
                    Console.WriteLine($"[Manager] Unknown message type: {dto.Type}");
                    break;
            }
        }

        private void TryPairPlayers()
        {
            if (_waiting.Count >= 2)
            {
                if (_waiting.TryDequeue(out var p1) && _waiting.TryDequeue(out var p2))
                {
                    var g = new Game(p1, p2, this, _db);
                    _games.Add(g);
                    _playerToGame[p1.Id] = g;
                    _playerToGame[p2.Id] = g;

                    var pairedPayload = JsonSerializer.SerializeToElement(new { message = $"Paired: {p1.Name} <-> {p2.Name}" });
                    _ = p1.SendAsync(new MessageDto { Type = "info", Payload = pairedPayload });
                    _ = p2.SendAsync(new MessageDto { Type = "info", Payload = pairedPayload });

                    Console.WriteLine($"[Manager] Paired: {p1.Name} <-> {p2.Name}");
                }
            }
        }

        public void GameEnded(Game g)
        {
            _playerToGame.TryRemove(g.Player1.Id, out _);
            _playerToGame.TryRemove(g.Player2.Id, out _);
            _games.Remove(g);
            _gameBots.TryRemove(g, out _);
            _smartByGame.TryRemove(g, out _);
            Console.WriteLine("[Manager] Game removed.");
        }

        // ===== BOT režimas =====

        private async Task CreateBotGameAsync(PlayerConnection human)
        {
            var botSocket = new NoopWebSocket();
            var bot = new PlayerConnection(botSocket, this) { Name = "NPC" };

            var game = new Game(human, bot, this, _db);
            _games.Add(game);
            _playerToGame[human.Id] = game;
            _playerToGame[bot.Id] = game;
            _gameBots[game] = bot;

            _smartByGame[game] = new SmartHuntTargetStrategy(10, 10);

            var info = JsonSerializer.SerializeToElement(new { message = $"Sukurta partija su botu: {human.Name} vs NPC" });
            await human.SendAsync(new MessageDto { Type = "info", Payload = info });

            var botShips = GenerateRandomShips();
            game.PlaceShips(bot.Id, botShips);
        }

        private List<ShipDto> GenerateRandomShips()
        {
            var rnd = new Random();
            var grid = new int[10, 10];
            var result = new List<ShipDto>();
            int[] lens = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            foreach (var len in lens)
            {
                bool placed = false;
                for (int tries = 0; tries < 250 && !placed; tries++)
                {
                    bool horiz = rnd.Next(2) == 0;
                    int x = rnd.Next(0, 10 - (horiz ? (len - 1) : 0));
                    int y = rnd.Next(0, 10 - (horiz ? 0 : (len - 1)));

                    bool ok = true;
                    for (int i = 0; i < len && ok; i++)
                    {
                        int cx = x + (horiz ? i : 0);
                        int cy = y + (horiz ? 0 : i);
                        if (grid[cy, cx] != 0) ok = false;
                    }
                    if (!ok) continue;

                    for (int i = 0; i < len; i++)
                    {
                        int cx = x + (horiz ? i : 0);
                        int cy = y + (horiz ? 0 : i);
                        grid[cy, cx] = 1;
                    }

                    result.Add(new ShipDto { X = x, Y = y, Len = len, Dir = horiz ? "H" : "V" });
                    placed = true;
                }
            }

            return result;
        }

        /// <summary> Botas šaudo kol pataiko; Miss atiduoda ėjimą žmogui. Rezultatą imam iš g.LastResult. </summary>
        private async Task TryBotChainAsync(Game g, PlayerConnection bot)
        {
            if (!_smartByGame.TryGetValue(g, out var strat))
            {
                strat = new SmartHuntTargetStrategy(10, 10);
                _smartByGame[g] = strat;
            }

            while (!g.IsOver && g.CurrentPlayerId == bot.Id)
            {
                var (tx, ty) = strat.NextShot();

                await g.ProcessShot(bot.Id, tx, ty); 

                 if (g.IsOver) break;

                // žinome tikslų rezultatą
                var outcome = g.LastResult switch
                {
                    "hit"               => ShotOutcome.Hit,
                    "whole_ship_down"   => ShotOutcome.Sunk,
                    _                   => ShotOutcome.Miss
                };

                strat.ObserveResult((tx, ty), outcome);

                await Task.Delay(250); // mažas pauzės efektas
            }
        }
    }

    // ===========================================================
    //  PROTINGA NPC STRATEGIJA: Hunt → Target → Linija (vidinė)
    // ===========================================================
    public enum ShotOutcome { Miss, Hit, Sunk }

    public sealed class SmartHuntTargetStrategy
    {
        private readonly int _w, _h;
        private readonly HashSet<(int x,int y)> _shot = new();
        private readonly HashSet<(int x,int y)> _hits = new();

        private readonly Queue<(int x,int y)> _frontier = new();
        private (int x,int y)? _seedHit = null;
        private (int dx,int dy)? _lineDir = null;
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

            // 3) Hunt – checkerboard, paskui fallback
            var rnd = Random.Shared;
            for (int i = 0; i < 200; i++)
            {
                int x = rnd.Next(0,_w), y = rnd.Next(0,_h);
                if (((x+y)&1)==0 && !_shot.Contains((x,y))) return (x,y);
            }
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
                return;

            if (outcome == ShotOutcome.Sunk)
            {
                // nuskandintas – NEBESHAUDOM aplink: išvalom visą "taikinį"
                _hits.Add(cell);
                _seedHit = null;
                _lineDir = null;
                _reverseTried = false;
                _frontier.Clear();
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
                    EnqueueNeighbors(cell);
                }
            }
        }

        // ===== helpers =====
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
