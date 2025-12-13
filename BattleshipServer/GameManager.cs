using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipServer.Data;
using BattleshipServer.Domain;
using BattleshipServer.Models;
using BattleshipServer.Npc;

// tavo šakos (CoR)
using BattleshipServer.MessageHandling;

// kita šaka (Visitor/State) – paliekam, kad “nieko neištrint”
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
        private readonly Dictionary<Guid, Game> copiedGames = new();

        // Facade (kaip buvo Prašau_1 šakoje)
        private readonly GameManagerFacade.GameManagerFacade gameManagerFacade =
            new GameManagerFacade.GameManagerFacade();

        // Chain of Responsibility (>= 4 handlers) – paliekam kaip pagrindinį apdorojimą
        private readonly IMessageHandler _messageChain;

        // Visitor pipeline paliekam (kad “neištrinti”), bet default – tik validacija+log
        private readonly bool _useVisitorValidationAndLogging = true;

        // Jei norėsi kada perjungti, kad Visitor darytų ir handling – nustatyk true.
        // Palieku false, kad nesidubliuotų su CoR (kitaip tas pats message būtų apdorotas 2 kartus).
        private readonly bool _useVisitorAsPrimaryHandler = false;

        public GameManager()
        {
            _messageChain = BuildMessageChain();
        }

        internal GameManagerFacade.GameManagerFacade Facade => gameManagerFacade;
        internal Database Db => _db;

        public void AddToWaitingQueue(PlayerConnection player) => _waiting.Enqueue(player);

        public Game? GetPlayersGame(Guid playerId)
            => _playerToGame.TryGetValue(playerId, out var game) ? game : null;

        public (Game? game, IBotPlayerController? bot) GetBotGame(Guid playerId)
            => _botGames.TryGetValue(playerId, out var bg) ? bg : (null, null);

        public void AddGame(Game game, Guid playerId)
        {
            _games.Add(game);
            _playerToGame[playerId] = game;
        }

        public void AddGame(Game game, Guid playerId, IBotPlayerController orchestrator)
        {
            AddGame(game, playerId);
            _botGames[playerId] = (game, orchestrator);
        }

        public async Task HandleMessageAsync(PlayerConnection player, MessageDto dto)
        {
            // 1) Visitor pipeline (validacija + log) – paliekam iš main šakos
            if (_useVisitorValidationAndLogging)
            {
                try
                {
                    var message = CreateGameMessage(dto);

                    IGameMessageVisitor validatorVisitor = new GameMessageValidatorVisitor();
                    IGameMessageVisitor logVisitor = new GameMessageLogVisitor();

                    await message.AcceptAsync(validatorVisitor, player);
                    await message.AcceptAsync(logVisitor, player);

                    // Jei kada norėsi, gali perjungti į Visitor-handling režimą
                    if (_useVisitorAsPrimaryHandler)
                    {
                        IGameMessageVisitor handlerVisitor = new GameMessageHandlerVisitor(this, _db);
                        await message.AcceptAsync(handlerVisitor, player);
                        return;
                    }
                }
                catch (ArgumentException)
                {
                    // Unknown message type – tegul apdoroja CoR (UnknownMessageHandler)
                }
            }

            // 2) Pagrindinis apdorojimas per Chain of Responsibility (Prašau_1 šaka)
            await _messageChain.HandleAsync(player, dto);
        }

        // Paliekam CreateGameMessage iš main šakos (Visitor)
        // Pastaba: bench/unknown čia specialiai neįdėti – juos sugauna CoR.
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

        // Pairing (paliekam async Task versiją)
        internal async Task TryPairPlayersAsync()
        {
            if (_waiting.Count >= 2)
            {
                if (_waiting.TryDequeue(out var p1) && _waiting.TryDequeue(out var p2))
                {
                    var g = new Game(p1, p2, this, _db);
                    _games.Add(g);
                    _playerToGame[p1.Id] = g;
                    _playerToGame[p2.Id] = g;

                    await Scoreboard.Instance.RegisterPlayers(p1.Name, p2.Name, g);

                    var pairedPayload = JsonSerializer.SerializeToElement(
                        new { message = $"Paired: {p1.Name} <-> {p2.Name}" });

                    _ = p1.SendAsync(new MessageDto { Type = "info", Payload = pairedPayload });
                    _ = p2.SendAsync(new MessageDto { Type = "info", Payload = pairedPayload });

                    Console.WriteLine($"[Manager] Paired: {p1.Name} <-> {p2.Name}");
                }
            }
        }

        // Wrapper, jei kažkur tavo kode kviečiamas senas sync pavadinimas
        public void TryPairPlayers() => _ = TryPairPlayersAsync();

        private IMessageHandler BuildMessageChain()
        {
            // Chain order, Unknown last
            IMessageHandler h1 = new RegisterMessageHandler(this);
            var h2 = h1.SetNext(new ReadyMessageHandler(this));
            var h3 = h2.SetNext(new CopyGameMessageHandler(this));
            var h4 = h3.SetNext(new UseGameCopyMessageHandler(this));
            var h5 = h4.SetNext(new ShotMessageHandler(this));
            var h6 = h5.SetNext(new PlayBotMessageHandler(this));
            var h7 = h6.SetNext(new PlaceShieldMessageHandler(this));
            var h8 = h7.SetNext(new BenchMessageHandler(this));
            h8.SetNext(new UnknownMessageHandler(this));
            return h1;
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

        public void StoreGameCopy(Guid playerId, Game game) => copiedGames[playerId] = game;

        public Game? GetCopiedGame(Guid playerId)
            => copiedGames.TryGetValue(playerId, out var game) ? game : null;
    }
}
