using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public async Task HandleMessageAsync(PlayerConnection player, MessageDto dto)
        {
            switch (dto.Type)
            {
                case "register":
                    if (dto.Payload.TryGetProperty("playerName", out var nmElem))
                    {
                        player.Name = nmElem.GetString();
                    }
                    _waiting.Enqueue(player);
                    Console.WriteLine($"[Manager] Player registered: {player.Name} ({player.Id})");
                    await player.SendAsync(new MessageDto { Type = "register", Payload = JsonDocument.Parse("{\"message\":\"registered\"}").RootElement });
                    TryPairPlayers();
                    break;

                case "ready":
                    if (_playerToGame.TryGetValue(player.Id, out var gReady))
                    {
                        // extract ships
                        var ships = new List<ShipDto>();
                        bool isStandartGame = true;
                        if (dto.Payload.TryGetProperty("isStandartGame", out JsonElement element))
                        {
                            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                            {
                                isStandartGame = element.GetBoolean();
                                gReady.SetGameMode(player.Id, isStandartGame);
                            }
                        }
                        if (dto.Payload.TryGetProperty("ships", out var shipsElem))
                        {
                            foreach (var el in shipsElem.EnumerateArray())
                            {
                                ships.Add(new ShipDto
                                {
                                    X = el.GetProperty("x").GetInt32(),
                                    Y = el.GetProperty("y").GetInt32(),
                                    Len = el.GetProperty("len").GetInt32(),
                                    Dir = el.GetProperty("dir").GetString()
                                });
                            }
                        }

                        gReady.PlaceShips(player.Id, ships);
                        Console.WriteLine($"[Manager] Player {player.Name} placed {ships.Count} ships.");
                        if (gReady.IsReady && gReady.GameModesMatch)
                        {
                            await gReady.StartGame();
                        } else if (!gReady.GameModesMatch)
                        {
                            Console.WriteLine("Game mode of players do not match! Try again");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Manager] Ready received but player not in a game yet.");
                    }
                    break;

                case "shot":
                    if (dto.Payload.TryGetProperty("x", out var xe) && dto.Payload.TryGetProperty("y", out var ye))
                    {
                        int x = xe.GetInt32();
                        int y = ye.GetInt32();
                        if (_playerToGame.TryGetValue(player.Id, out var gShot))
                        {
                            await gShot.ProcessShot(player.Id, x, y);
                        }
                    }
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
            Console.WriteLine("[Manager] Game removed.");
        }
    }
}
