﻿﻿using BattleshipServer.Data;
using BattleshipServer.Domain;
using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer
{
    public class Game
    {
        public Player Player1 { get; }
        public Player Player2 { get; }

        public Guid CurrentPlayerId { get; private set; }
        public bool IsOver { get; private set; } = false;

        // Boto strategijai / diagnostikai
        public (int x, int y) LastShot { get; private set; } = (-1, -1);
        public string LastResult { get; private set; } = "miss"; // "miss" | "hit" | "whole_ship_down"

        // Nauja: visi paskutinio nuskendusio laivo langeliai (jei buvo)
        public IReadOnlyList<Coordinate> LastSunkCells { get; private set; } = Array.Empty<Coordinate>();

        private readonly GameManager _manager;
        private readonly Database _db;

        public bool IsReady => _p1Placed && _p2Placed;

        private bool _p1Placed;
        private bool _p2Placed;

        public Game(PlayerConnection p1, PlayerConnection p2, GameManager manager, Database db)
        {
            Player1 = new Player(p1);
            Player2 = new Player(p2);
            _manager = manager;
            _db = db;
        }

        public void PlaceShips(Guid playerId, List<ShipDto> ships)
        {
            if (playerId == Player1.Id)
            {
                Player1.Board.PlaceShips(ships);
                _p1Placed = true;
                _db.SaveMap(Player1.Id.ToString(), JsonSerializer.Serialize(ships));
            }
            else
            {
                Player2.Board.PlaceShips(ships);
                _p2Placed = true;
                _db.SaveMap(Player2.Id.ToString(), JsonSerializer.Serialize(ships));
            }
        }

        public async Task StartGame()
        {
            CurrentPlayerId = Player1.Id;

            var p1Payload = JsonSerializer.SerializeToElement(new
            {
                opponent = Player2.Name,
                yourId = Player1.Id.ToString(),
                opponentId = Player2.Id.ToString(),
                current = CurrentPlayerId.ToString()
            });
            var p2Payload = JsonSerializer.SerializeToElement(new
            {
                opponent = Player1.Name,
                yourId = Player2.Id.ToString(),
                opponentId = Player1.Id.ToString(),
                current = CurrentPlayerId.ToString()
            });

            await Player1.Conn.SendAsync(new MessageDto { Type = "startGame", Payload = p1Payload });
            await Player2.Conn.SendAsync(new MessageDto { Type = "startGame", Payload = p2Payload });

            Console.WriteLine($"[Game] Started: {Player1.Name} vs {Player2.Name}");
        }

        public async Task ProcessShot(Guid shooterId, int x, int y)
        {
            if (IsOver)
            {
                await GetConn(shooterId).SendAsync(new MessageDto
                {
                    Type = "error",
                    Payload = JsonDocument.Parse("{\"message\":\"Game already finished\"}").RootElement
                });
                return;
            }

            if (shooterId != CurrentPlayerId)
            {
                await GetConn(shooterId).SendAsync(new MessageDto
                {
                    Type = "error",
                    Payload = JsonDocument.Parse("{\"message\":\"Not your turn\"}").RootElement
                });
                return;
            }

            var shooter = (shooterId == Player1.Id) ? Player1 : Player2;
            var target  = (shooterId == Player1.Id) ? Player2 : Player1;

            // pritaikom šūvį
            var outcome = target.Board.ApplyShot(x, y);

            // transliuojam pagrindinį rezultato langelį
            string resString = outcome.Kind switch
            {
                ShotKind.Miss => "miss",
                ShotKind.Hit  => "hit",
                ShotKind.Sunk => "whole_ship_down",
                _ => "miss"
            };

            var shotResult = JsonSerializer.SerializeToElement(new
            {
                x,
                y,
                result = resString,
                shooterId = shooterId.ToString(),
                targetId = target.Id.ToString()
            });

            await Player1.Conn.SendAsync(new MessageDto { Type = "shotResult", Payload = shotResult });
            await Player2.Conn.SendAsync(new MessageDto { Type = "shotResult", Payload = shotResult });

            // jei nuskendo – papildomai „whole_ship_down“ visiems laivo langeliams
            if (outcome.Kind == ShotKind.Sunk && outcome.SunkCells != null)
            {
                foreach (var (sx, sy) in outcome.SunkCells)
                {
                    var upd = JsonSerializer.SerializeToElement(new
                    {
                        x = sx,
                        y = sy,
                        result = "whole_ship_down",
                        shooterId = shooterId.ToString(),
                        targetId = target.Id.ToString()
                    });
                    await Player1.Conn.SendAsync(new MessageDto { Type = "shotResult", Payload = upd });
                    await Player2.Conn.SendAsync(new MessageDto { Type = "shotResult", Payload = upd });
                }

                // atnaujinam diagnostiką: visi nuskendusio laivo taškai
                LastSunkCells = outcome.SunkCells;
            }
            else
            {
                LastSunkCells = Array.Empty<Coordinate>();
            }

            // užpildom diagnostiką
            LastShot = (x, y);
            LastResult = resString;

            // pabaiga?
            bool gameOver = target.Board.AllShipsSunk();
            if (gameOver)
            {
                IsOver = true;
                var winner = shooterId.ToString();
                var go = JsonSerializer.SerializeToElement(new { winnerId = winner });

                await Player1.Conn.SendAsync(new MessageDto { Type = "gameOver", Payload = go });
                await Player2.Conn.SendAsync(new MessageDto { Type = "gameOver", Payload = go });

                _manager.GameEnded(this);
                _db.SaveGame(Player1.Name, Player2.Name, winner);
                Console.WriteLine($"[Game] Game over. Winner: {winner}");
                return;
            }

            // ėjimo keitimas – tik kai Miss (taip buvo ir anksčiau)
            if (outcome.Kind == ShotKind.Miss)
                CurrentPlayerId = target.Id;

            var turnPayload = JsonSerializer.SerializeToElement(new { current = CurrentPlayerId.ToString() });
            await Player1.Conn.SendAsync(new MessageDto { Type = "turn", Payload = turnPayload });
            await Player2.Conn.SendAsync(new MessageDto { Type = "turn", Payload = turnPayload });
        }

        private PlayerConnection GetConn(Guid id) => id == Player1.Id ? Player1.Conn : Player2.Conn;
    }
}
