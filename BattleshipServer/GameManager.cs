﻿using System;
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


namespace BattleshipServer
{
    public class GameManager
    {
        private readonly ConcurrentQueue<PlayerConnection> _waiting = new();
        private readonly ConcurrentDictionary<Guid, Game> _playerToGame = new();
        private readonly List<Game> _games = new();
        private readonly Database _db = new Database("battleship.db"); 
        private readonly ConcurrentDictionary<Guid, (Game game, IBotPlayerController bot)> _botGames = new();
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
        }

        public void AddGame(Game game, Guid playerId, IBotPlayerController orchestrator)
        {
            this.AddGame(game, playerId);
            _botGames[playerId] = (game, orchestrator);
        }

        public async Task HandleMessageAsync(PlayerConnection player, MessageDto dto)
        {
            GameMessage message = CreateGameMessage(dto);
            IGameMessageVisitor validatorVisitor = new GameMessageValidatorVisitor();
            IGameMessageVisitor messageHandlerVisitor = new GameMessageHandlerVisitor(this, _db);

            await message.AcceptAsync(validatorVisitor, player);
            await message.AcceptAsync(messageHandlerVisitor, player);
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

                    // Show player names on scoreboard
                    await Scoreboard.Instance.RegisterPlayers(p1.Name, p2.Name, g);

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
    }
}