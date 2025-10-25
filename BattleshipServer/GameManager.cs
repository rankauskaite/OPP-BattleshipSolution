using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipServer.Models;
using BattleshipServer.Data;
using System.Xml.Linq; 
using BattleshipServer.Npc;
using System.Net.WebSockets; 
using BattleshipServer.Builders;
using BattleshipServer.Domain;


namespace BattleshipServer
{
    public class GameManager
    {
        private readonly ConcurrentQueue<PlayerConnection> _waiting = new();
        private readonly ConcurrentDictionary<Guid, Game> _playerToGame = new();
        private readonly List<Game> _games = new();
        private readonly Database _db = new Database("battleship.db"); 
        private readonly ConcurrentDictionary<Guid, (Game game, BotOrchestrator bot)> _botGames = new();
        private readonly Dictionary<Guid, Game> copiedGames = new();
        private readonly GameManagerFacade.GameManagerFacade gameManagerFacade = new GameManagerFacade.GameManagerFacade();

        public void AddToWaitingQueue(PlayerConnection player)
        {
            _waiting.Enqueue(player);
        }

        public Game? GetPlayersGame(Guid playerId)
        {
            if (_playerToGame.TryGetValue(playerId, out var game))
            {
                return game;
            }
            return null;
        }

        public (Game? game, BotOrchestrator? bot) GetBotGame(Guid playerId)
        {
            if (_botGames.TryGetValue(playerId, out var bg))
            {
                return bg;
            }
            return (null, null);
        }

        public void AddGame(Game game, Guid playerId)
        {
            _games.Add(game);
            _playerToGame[playerId] = game;
        }

        public void AddGame(Game game, Guid playerId, BotOrchestrator orchestrator)
        {
            this.AddGame(game, playerId);
            _botGames[playerId] = (game, orchestrator);
        }

        public async Task HandleMessageAsync(PlayerConnection player, MessageDto dto)
        {
            switch (dto.Type)
            {
                case "register":
                    await gameManagerFacade.RegisterPlayerAsync(this, player, dto);
                    TryPairPlayers();
                    break;

                case "ready":
                    await gameManagerFacade.MarkPlayerAsReady(this, player, dto);
                    break;
                case "copyGame":
                    await gameManagerFacade.CopyGame(this, player);
                    break;
                case "useGameCopy":
                    await gameManagerFacade.UseGameCopy(this, player);
                    break;
                case "shot":
                    await gameManagerFacade.HandleShot(this, player, dto);
                    break;
                case "playBot":
                    gameManagerFacade.HandlePlayBot(this, player, dto, _db);
                    break;
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

                    // Notify both that they were paired
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
            _botGames.TryRemove(g.Player1.Id, out _);
            _botGames.TryRemove(g.Player2.Id, out _);
            Console.WriteLine("[Manager] Game removed.");
        }

        public void StoreGameCopy(Guid playerId, Game game)
        {
            copiedGames[playerId] = game;
        }

        public Game? GetCopiedGame(Guid playerId)
        {
            if (copiedGames.TryGetValue(playerId, out var game))
            {
                return game;
            }
            return null;
        }


        // private static List<ShipDto> RandomFleet(bool standart)
        // {
        //     var lens = standart ? new[] {4, 3, 3, 2, 2, 2, 1, 1, 1, 1} : new[] {3, 2, 2, 2, 1};
        //     var rnd = new Random();
        //     var used = new int[10,10];
        //     var list = new List<ShipDto>();

        //     foreach (var L in lens)
        //     {
        //         bool placed = false;
        //         for (int tries=0; tries<500 && !placed; tries++)
        //         {
        //             bool horiz = rnd.Next(2)==0;
        //             int x = rnd.Next(0, 10 - (horiz ? L : 0));
        //             int y = rnd.Next(0, 10 - (horiz ? 0 : L));
        //             if (CanPlace(used, x, y, L, horiz))
        //             {
        //                 for (int i=0;i<L;i++)
        //                 {
        //                     int cx = x + (horiz? i:0);
        //                     int cy = y + (horiz? 0:i);
        //                     used[cy, cx] = 1;
        //                 }
        //                 list.Add(new ShipDto { X=x, Y=y, Len=L, Dir=horiz?"H":"V" });
        //                 placed = true;
        //             }
        //         }
        //     }
        //     return list;

        //     static bool CanPlace(int[,] b, int x, int y, int len, bool h)
        //     {
        //         for (int i=0;i<len;i++)
        //         {
        //             int cx = x + (h? i:0);
        //             int cy = y + (h? 0:i);
        //             if (cx<0||cx>=10||cy<0||cy>=10) return false;
        //             if (b[cy,cx]!=0) return false;
        //         }
        //         return true;
        //     }
        // }
    }
}
