﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipServer.Data;
using BattleshipServer.Models;      // MessageDto, ShipDto, BoardKnowledge
using BattleshipServer.Npc;         // NpcController, INpcShotStrategy, RuleBasedSelector, ShotStrategyFactory
using BattleshipServer.Domain;

namespace BattleshipServer
{
    public class GameManager
    {
        private readonly ConcurrentQueue<PlayerConnection> _waiting = new();
        private readonly ConcurrentDictionary<Guid, Game> _playerToGame = new();
        private readonly List<Game> _games = new();
        private readonly Database _db = new Database("battleship.db");

        // Bot'o "jungtis" per žaidimą
        private readonly ConcurrentDictionary<Game, PlayerConnection> _gameBots = new();

        // NPC komponentai per žaidimą
        private readonly ConcurrentDictionary<Game, BoardKnowledge> _botKnowledge = new();
        private readonly ConcurrentDictionary<Game, NpcController>  _botController = new();

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
                        // surenkame laivus iš payload
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

                            // jei pirmas ėjimas botui – pajudinam iškart
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

                            // po žaidėjo ėjimo – jei botui eilė, paleidžiam jo grandinę
                            if (!gShot.IsOver && _gameBots.TryGetValue(gShot, out var bot) && gShot.CurrentPlayerId == bot.Id)
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
            _playerToGame.TryRemove(g.Player1.Conn.Id, out _);
            _playerToGame.TryRemove(g.Player2.Conn.Id, out _);
            _games.Remove(g);

            _gameBots.TryRemove(g, out _);
            _botKnowledge.TryRemove(g, out _);
            _botController.TryRemove(g, out _);

            Console.WriteLine("[Manager] Game removed.");
        }

        // ================= BOT režimas =================

        private async Task CreateBotGameAsync(PlayerConnection human)
        {
            var botSocket = new NoopWebSocket();
            var bot = new PlayerConnection(botSocket, this) { Name = "NPC" };

            var game = new Game(human, bot, this, _db);
            _games.Add(game);
            _playerToGame[human.Id] = game;
            _playerToGame[bot.Id] = game;
            _gameBots[game] = bot;

            // NPC komponentai šiam žaidimui
            _botKnowledge[game] = new BoardKnowledge(10, 10);

            // Dinaminis perjungimas: controller + selector + pradinė strategija per fabriką
            _botController[game] = new NpcController(new RuleBasedSelector(), ShotStrategyFactory.Create("human-like"));

            var info = JsonSerializer.SerializeToElement(new { message = $"Sukurta partija su botu: {human.Name} vs NPC" });
            await human.SendAsync(new MessageDto { Type = "info", Payload = info });

            // Bot'ui automatiškai išdėstom laivus
            var botShips = GenerateRandomShips();
            game.PlaceShips(bot.Id, botShips);
        }

        /// <summary> Paprastas atsitiktinis 10 laivų rinkinys (4,3,3,2,2,2,1,1,1,1) į 10x10, be sudėtingų taisyklių. </summary>
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

        /// <summary>
        /// Botas šaudo kol prameta. Strategija: NpcController + BoardKnowledge.
        /// </summary>
        private async Task TryBotChainAsync(Game g, PlayerConnection bot)
        {
            if (!_botController.TryGetValue(g, out var ctl) || !_botKnowledge.TryGetValue(g, out var know))
            {
                // saugiklis – jei dėl kokios nors priežasties neinicijuota
                know = new BoardKnowledge(10, 10);
                ctl  = new NpcController(new RuleBasedSelector(), ShotStrategyFactory.Create("human-like"));
                _botKnowledge[g] = know;
                _botController[g] = ctl;
            }

            while (!g.IsOver && g.CurrentPlayerId == bot.Id)
            {
                // 1) NPC pasirenka taikinį pagal žinias (čia gali įvykti STRATEGIJOS PASIKEITIMAS)
                var (tx, ty) = ctl.Decide(know);

                // 2) Paleidžiam šūvį (Game atsiųs shotResult ir, jei Sunk, papildomas "whole_ship_down")
                await g.ProcessShot(bot.Id, tx, ty);

                if (g.IsOver) break;

                // 3) Atnaujinam žinias iš paskutinio rezultato
                switch (g.LastResult)
                {
                    case "hit":
                        know.MarkHit(tx, ty);
                        break;

                    case "whole_ship_down":
                        // pažymim pagrindinį tašką
                        know.MarkSunk(tx, ty);
                        // ir VISUS Sunk langelius (nauja)
                        if (g.LastSunkCells != null)
                        {
                            foreach (var c in g.LastSunkCells)
                                know.MarkSunk(c.X, c.Y);
                        }
                        break;

                    default:
                        know.MarkMiss(tx, ty);
                        break;
                }

                // 4) nedidelė pauzė, kad matytųsi animacija
                await Task.Delay(250);
            }
        }
    }
}
