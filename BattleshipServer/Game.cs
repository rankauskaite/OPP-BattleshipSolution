﻿using BattleshipServer.Data;
using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer
{
    public class Game
    {
        public PlayerConnection Player1 { get; }
        public PlayerConnection Player2 { get; }
        public Guid CurrentPlayerId { get; private set; }

        // Nauja: paskutinio šūvio info (naudojama tik servero botei)
        public (int x, int y) LastShot { get; private set; } = (-1, -1);
        public string LastResult { get; private set; } = "miss"; // "miss" | "hit" | "whole_ship_down"

        private readonly int[,] _board1 = new int[10, 10];
        private readonly int[,] _board2 = new int[10, 10];
        private readonly List<Ship> _ships1 = new();
        private readonly List<Ship> _ships2 = new();
        private readonly GameManager _manager;
        private readonly Database _db; 
        public bool IsOver { get; private set; } = false;


        public bool IsReady => _ships1.Count > 0 && _ships2.Count > 0;

        public Game(PlayerConnection p1, PlayerConnection p2, GameManager manager, Database db)
        {
            Player1 = p1;
            Player2 = p2;
            _manager = manager;
            _db = db;
        }

        public void PlaceShips(Guid playerId, List<ShipDto> shipsDto)
        {
            var board = playerId == Player1.Id ? _board1 : _board2;
            var ships = playerId == Player1.Id ? _ships1 : _ships2;

            // reset board & ships
            Array.Clear(board, 0, board.Length);
            ships.Clear();

            foreach (var sd in shipsDto)
            {
                var ship = new Ship(sd.X, sd.Y, sd.Len, sd.Dir?.ToUpper() == "H");
                ships.Add(ship);
                for (int i = 0; i < sd.Len; i++)
                {
                    int cx = sd.X + (ship.Horizontal ? i : 0);
                    int cy = sd.Y + (ship.Horizontal ? 0 : i);
                    if (cx >= 0 && cx < 10 && cy >= 0 && cy < 10)
                        board[cy, cx] = 1;
                }
            }

            var mapJson = JsonSerializer.Serialize(shipsDto);
            _db.SaveMap(playerId.ToString(), mapJson);
        }

        public async Task StartGame()
        {
            CurrentPlayerId = Player1.Id; // p1 starts

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

            await Player1.SendAsync(new Models.MessageDto { Type = "startGame", Payload = p1Payload });
            await Player2.SendAsync(new Models.MessageDto { Type = "startGame", Payload = p2Payload });

            Console.WriteLine($"[Game] Started: {Player1.Name} vs {Player2.Name}");
        }

        public async Task ProcessShot(Guid shooterId, int x, int y)
        { 
            if (IsOver)
            {
                await GetPlayer(shooterId).SendAsync(new Models.MessageDto {
                    Type = "error",
                    Payload = JsonDocument.Parse("{\"message\":\"Game already finished\"}").RootElement
                });
                return;
            }

            if (shooterId != CurrentPlayerId)
            {
                await GetPlayer(shooterId).SendAsync(new Models.MessageDto
                {
                    Type = "error",
                    Payload = JsonDocument.Parse("{\"message\":\"Not your turn\"}").RootElement
                });
                return;
            }

            var shooter = GetPlayer(shooterId);
            var target = GetOpponent(shooterId);
            var targetBoard = target == Player1 ? _board1 : _board2;
            var targetShips = target == Player1 ? _ships1 : _ships2;

            bool hit = false;
            bool gameOver = false;
            bool wholeDownTriggered = false;

            if (x < 0 || x >= 10 || y < 0 || y >= 10)
            {
                await shooter.SendAsync(new Models.MessageDto
                {
                    Type = "error",
                    Payload = JsonDocument.Parse("{\"message\":\"Invalid coords\"}").RootElement
                });
                return;
            }

            if (targetBoard[y, x] == 1)
            {
                targetBoard[y, x] = 3; // hit
                hit = true;
                Scoreboard.Instance.AddHit(shooterId, Player1.Id, Player2.Id);
            }
            else if (targetBoard[y, x] == 0)
            {
                targetBoard[y, x] = 2; // miss
                hit = false;
            }
            else
            {
                await shooter.SendAsync(new Models.MessageDto
                {
                    Type = "error",
                    Payload = JsonDocument.Parse("{\"message\":\"Cell already shot\"}").RootElement
                });
                return;
            }

            // Ship sink check
            // --- Update ship sink status and check overall survival (REPLACED) ---
            bool anyLeft = false;
            bool wholeDown = false;

            foreach (var s in targetShips)
            {
                bool sunkNow = s.IsSunk(targetBoard);

                if (!sunkNow)
                {
                    anyLeft = true; // dar yra bent vienas gyvas laivas
                }
                else if (!s.MarkedSunk) // tik pirmą kartą, kai tikrai nuskendo
                {
                    s.setAsSunk(targetBoard);
                    bool containsShot = s.Horizontal
                        ? (y == s.Y && x >= s.X && x < s.X + s.Len)
                        : (x == s.X && y >= s.Y && y < s.Y + s.Len);
                    if (containsShot)
                        wholeDownTriggered = true;

                    // Ištransliuojam "whole_ship_down" tik per to laivo langelius
                    for (int i = 0; i < s.Len; i++)
                    {
                        int cx1 = s.X + (s.Horizontal ? i : 0);
                        int cy1 = s.Y + (s.Horizontal ? 0 : i);
                        if (cx1 < 0 || cx1 >= 10 || cy1 < 0 || cy1 >= 10) break;

                        var updateBoard = JsonSerializer.SerializeToElement(new
                        {
                            x = cx1,
                            y = cy1,
                            result = "whole_ship_down",
                            shooterId = shooterId.ToString(),
                            targetId = target.Id.ToString()
                        });

                        await Player1.SendAsync(new MessageDto { Type = "shotResult", Payload = updateBoard });
                        await Player2.SendAsync(new MessageDto { Type = "shotResult", Payload = updateBoard });
                    }
                }
            }

            if (!anyLeft) gameOver = true;


            // bendras pranešimas apie konkretų langelį
            var resString = hit && !wholeDownTriggered ? "hit" :
                            hit &&  wholeDownTriggered ? "whole_ship_down" : "miss";

            var shotResult = JsonSerializer.SerializeToElement(new
            {
                x,
                y,
                result = resString,
                shooterId = shooterId.ToString(),
                targetId = target.Id.ToString()
            });

            await Player1.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult });
            await Player2.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult });

            // --- nauja: atnaujinam paskutinio šūvio informaciją ---
            LastShot = (x, y);
            LastResult = resString;

            if (gameOver)
            {
                IsOver = true; // ← svarbu: po šito nebeleisime ėjimų

                var winner = shooterId.ToString();
                var goPayload = JsonSerializer.SerializeToElement(new { winnerId = winner });
                Scoreboard.Instance.AddWin(shooterId, Player1.Id, Player2.Id);
                await Player1.SendAsync(new Models.MessageDto { Type = "gameOver", Payload = goPayload });
                await Player2.SendAsync(new Models.MessageDto { Type = "gameOver", Payload = goPayload });

                _manager.GameEnded(this);
                _db.SaveGame(Player1.Name ?? Player1.Id.ToString(), Player2.Name ?? Player2.Id.ToString(), shooterId.ToString());
                Console.WriteLine($"[Game] Game over. Winner: {winner}");
                return; // ← daugiau nieko nebedarom
            }
            else
            {
                // ėjimą perjungiam TIK kai prameta
                if (!hit)
                    CurrentPlayerId = target.Id;

                var turnPayload = JsonSerializer.SerializeToElement(new { current = CurrentPlayerId.ToString() });
                await Player1.SendAsync(new Models.MessageDto { Type = "turn", Payload = turnPayload });
                await Player2.SendAsync(new Models.MessageDto { Type = "turn", Payload = turnPayload });
            }
        }

        private PlayerConnection GetPlayer(Guid id) => id == Player1.Id ? Player1 : Player2;
        private PlayerConnection GetOpponent(Guid id) => id == Player1.Id ? Player2 : Player1;
    }

    internal class Ship
    {
        public int X { get; }
        public int Y { get; }
        public int Len { get; }
        public bool Horizontal { get; }
        public bool MarkedSunk { get; private set; }  // <-- nauja

        public Ship(int x, int y, int len, bool horizontal)
        {
            X = x; Y = y; Len = len; Horizontal = horizontal;
            MarkedSunk = false;
        }

        public bool IsSunk(int[,] board)
        {
            for (int i = 0; i < Len; i++)
            {
                int cx = X + (Horizontal ? i : 0);
                int cy = Y + (Horizontal ? 0 : i);
                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10) return false;

                // BUVO: if (board[cy, cx] != 3) return false;
                // DABAR: leidžiam ir 3 (hit), ir 4 (sunk)
                var cell = board[cy, cx];
                if (cell != 3 && cell != 4) return false;
            }
            return true;
        }


        public void setAsSunk(int[,] board)
        {
            for (int i = 0; i < Len; i++)
            {
                int cx = X + (Horizontal ? i : 0);
                int cy = Y + (Horizontal ? 0 : i);
                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10) return;
                board[cy, cx] = 4;
            }
            MarkedSunk = true;  // <-- užfiksuojam
        }
    }
}
