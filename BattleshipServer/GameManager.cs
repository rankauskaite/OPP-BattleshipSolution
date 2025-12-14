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
using BattleshipServer.Visitor;
using BattleshipServer.State;

namespace BattleshipServer
{
    public class GameManager
    {
        private readonly ConcurrentQueue<PlayerConnection> _waiting = new();
        private readonly ConcurrentDictionary<Guid, Game> _playerToGame = new();
        private readonly List<Game> _games = new();
        private readonly Database _db = new Database("battleship.db");
        private readonly ConcurrentDictionary<Guid, (Game game, IBotPlayerController bot)> _botGames = new();
        private readonly ConcurrentDictionary<string, Game> _nameToGame = new();
        private readonly ConcurrentDictionary<string, IGameMemento> copiedGames = new();

        private string GetPlayerKey(string? name)
            => string.IsNullOrWhiteSpace(name) ? string.Empty : name;

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

        public (Game? game, IBotPlayerController? bot) GetBotGame(Guid playerId)
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

            if (!string.IsNullOrWhiteSpace(game.Player1.Name))
                _nameToGame[game.Player1.Name] = game;
            if (!string.IsNullOrWhiteSpace(game.Player2.Name))
                _nameToGame[game.Player2.Name] = game;
        }

        public void AddGame(Game game, Guid playerId, IBotPlayerController orchestrator)
        {
            this.AddGame(game, playerId);
            _botGames[playerId] = (game, orchestrator);
        }

        public async Task HandleMessageAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "bench")
                return;

            GameMessage message = CreateGameMessage(dto);

            IGameMessageVisitor validatorVisitor = new GameMessageValidatorVisitor();
            IGameMessageVisitor messageHandlerVisitor = new GameMessageHandlerVisitor(this, _db);
            IGameMessageVisitor gameMessageLogVisitor = new GameMessageLogVisitor();

            await message.AcceptAsync(validatorVisitor, player);
            await message.AcceptAsync(messageHandlerVisitor, player);
            await message.AcceptAsync(gameMessageLogVisitor, player);
        }

        private GameMessage CreateGameMessage(MessageDto dto)
        {
            return dto.Type switch
            {
                "register" => new RegisterGameMessage(dto),
                "ready" => new ReadyMessage(dto),
                "copyGame" => new CopyGameMessage(dto),
                "useGameCopy" => new UseGameCopyMessage(dto),
                "shot" => new ShotMessage(dto),
                "playBot" => new PlayBotMessage(dto),
                "placeShield" => new PlaceShieldMessage(dto),
                "healShip" => new HealShipMessage(dto),
                _ => throw new ArgumentException($"Unknown message type: {dto.Type}"),
            };
        }

        public async void TryPairPlayers()
        {
            if (_waiting.Count >= 2)
            {
                if (_waiting.TryDequeue(out var p1) && _waiting.TryDequeue(out var p2))
                {
                    var g = new Game(p1, p2, this, _db);
                    _games.Add(g);
                    _playerToGame[p1.Id] = g;
                    _playerToGame[p2.Id] = g;

                    if (!string.IsNullOrWhiteSpace(p1.Name))
                        _nameToGame[p1.Name] = g;
                    if (!string.IsNullOrWhiteSpace(p2.Name))
                        _nameToGame[p2.Name] = g;

                    await Scoreboard.Instance.RegisterPlayers(p1.Name, p2.Name, g);

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

            if (!string.IsNullOrWhiteSpace(g.Player1.Name))
            {
                _nameToGame.TryRemove(g.Player1.Name, out _);
                ClearGameCopyByName(g.Player1.Name);
            }

            if (!string.IsNullOrWhiteSpace(g.Player2.Name))
            {
                _nameToGame.TryRemove(g.Player2.Name, out _);
                ClearGameCopyByName(g.Player2.Name);
            }

            Console.WriteLine("[Manager] Game removed.");
        }

        // --- MEMENTO: autosave API ---
        public void StoreLatestSnapshotForGame(Game game)
        {
            var snapshot = game.CreateMemento();

            StoreGameCopy(game.Player1, snapshot);
            StoreGameCopy(game.Player2, snapshot);
        }

        public void StoreGameCopy(PlayerConnection player, IGameMemento memento)
        {
            var key = GetPlayerKey(player.Name);
            if (string.IsNullOrEmpty(key)) return;

            copiedGames[key] = memento;
            Console.WriteLine($"[MEMENTO] StoreGameCopy for {key}");
        }

        public IGameMemento? GetCopiedGameByPlayerName(string? name)
        {
            var key = GetPlayerKey(name);
            if (string.IsNullOrEmpty(key)) return null;

            if (copiedGames.TryGetValue(key, out var memento))
                return memento;

            return null;
        }

        public void ClearGameCopyByName(string? name)
        {
            var key = GetPlayerKey(name);
            if (string.IsNullOrEmpty(key)) return;

            copiedGames.TryRemove(key, out _);
        }

        public void RegisterExistingConnection(PlayerConnection player, Game game)
        {
            _playerToGame[player.Id] = game;
        }

        public Game? GetGameByPlayerName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (_nameToGame.TryGetValue(name, out var g) && !g.GetIsGameOver())
                return g;

            return null;
        }

        public void HandlePlayerDisconnected(PlayerConnection player)
        {
            Console.WriteLine($"[Disconnect] Player {player.Name} ({player.Id}) disconnected.");

            if (_playerToGame.TryGetValue(player.Id, out var game))
            {
                _playerToGame.TryRemove(player.Id, out _);

                bool otherStillInGame = false;
                foreach (var kv in _playerToGame)
                {
                    if (ReferenceEquals(kv.Value, game))
                    {
                        otherStillInGame = true;
                        break;
                    }
                }

                if (!otherStillInGame)
                {
                    Console.WriteLine("[Disconnect] This was the last player in the game. Ending game and clearing snapshots.");
                    GameEnded(game);
                }
                else
                {
                    Console.WriteLine("[Disconnect] Opponent is still connected, game kept alive for possible reconnect.");
                }
            }
            else
            {
                Console.WriteLine($"[Disconnect] Player {player.Name} is not mapped to any active game.");
            }

            ClearGameCopyByName(player.Name);
        }
    }
}