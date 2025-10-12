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


       
  

        public bool IsReady => _p1Placed && _p2Placed;

        private bool _p1Placed;
        private bool _p2Placed;

        private readonly int[,] _board1 = new int[10, 10];
        private readonly int[,] _board2 = new int[10, 10];
        private bool isStandartGame1 = true;
        private bool isStandartGame2 = true;
        private readonly List<Ship> _ships1 = new();
        private readonly List<Ship> _ships2 = new();
        private readonly GameManager _manager;
        private readonly Database _db;

        public bool IsReady => _ships1.Count > 0 && _ships2.Count > 0;
        public bool GameModesMatch => isStandartGame1 == isStandartGame2;


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

        public void SetGameMode(Guid playerId, bool isStandartGameVal)
        {
            bool isStandartGame = playerId == Player1.Id ? isStandartGame1 : isStandartGame2;
            if(playerId == Player1.Id) isStandartGame1 = isStandartGameVal;
            else isStandartGame2 = isStandartGameVal;
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

        public async Task ProcessShot(Guid shooterId, int x, int y, bool isDoubleBomb)
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

            var shooter = GetPlayer(shooterId);
            var target = GetOpponent(shooterId);
            var targetBoard = target == Player1 ? _board1 : _board2;
            var targetShips = target == Player1 ? _ships1 : _ships2;

            bool hit = false;
            bool success = false;
            bool gameOver = false;

            if (x < 0 || x >= 10 || y < 0 || y >= 10)

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

            if (isDoubleBomb)
            {
                int[] doubleBombNextCoors = GetDoubleBombCoords(targetBoard, x, y);
                int x1 = doubleBombNextCoors[0];
                int y1 = doubleBombNextCoors[1];
                if (doubleBombNextCoors.Length == 2 && x1 >= 0 && y1 >= 0)
                {
                    (success, hit) = ProcessShot(x1, y1, targetBoard);
                    if (!success)
                    {
                        await shooter.SendAsync(new Models.MessageDto { Type = "error", Payload = JsonDocument.Parse("{\"message\":\"Cell already shot\"}").RootElement });
                        return;
                    }
                    var shotResult1 = JsonSerializer.SerializeToElement(new { x=x1, y=y1, result = hit ? "hit" : "miss", shooterId = shooterId.ToString(), targetId = target.Id.ToString() });
                    await Player1.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult1 });
                    await Player2.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult1 });

                }
            }
            (success, hit) = ProcessShot(x, y, targetBoard);
            if (!success)
            {
                await shooter.SendAsync(new Models.MessageDto { Type = "error", Payload = JsonDocument.Parse("{\"message\":\"Cell already shot\"}").RootElement });
                return;
            }


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

            else
            {
                // change turn only on miss
                if (!hit)
                {
                    CurrentPlayerId = target.Id;
                }
                var turnPayload = JsonSerializer.SerializeToElement(new { current = CurrentPlayerId.ToString() });
                await Player1.SendAsync(new Models.MessageDto { Type = "turn", Payload = turnPayload });
                await Player2.SendAsync(new Models.MessageDto { Type = "turn", Payload = turnPayload });
            }
        }

        private (bool, bool) ProcessShot(int x, int y, int[,] targetBoard)
        {
            bool success = true;
            bool hit = false;
            if (targetBoard[y, x] == 1)
            {
                targetBoard[y, x] = 3; // hit
                hit = true;
            }
            else if (targetBoard[y, x] == 0)
            {
                targetBoard[y, x] = 2; // miss
                hit = false;
            }
            else
            {
                // already shot here
                success = false;
            }

            return (success, hit);
        }

        private int[] GetDoubleBombCoords(int[,] targetBoard, int x, int y)
        {
            int[] res = new int[4];
            List<int[]> possible_moves = new List<int[]>();

            if (y > 0 && (targetBoard[y-1, x] == 0 || targetBoard[y-1, x] == 1))
            {
                // second bomb drop is above current shot
                possible_moves.Add([x, y - 1,]);
            }

            if (y < targetBoard.GetLength(0) - 1 && (targetBoard[y + 1, x] == 0 || targetBoard[y + 1, x] == 1))
            {
                // second bobm drop is below current shot
                possible_moves.Add([x, y + 1]);
            }

            if(x > 0 && (targetBoard[y, x - 1] == 0 || targetBoard[y, x - 1] == 1))
            {
                // second bomb drop is to the left of the current shot
                possible_moves.Add([x - 1, y]);
            }

            if (x < targetBoard.GetLength(1) - 1)
            {
                // second bomb drop is to the right of the current shot
                possible_moves.Add([x + 1, y]);
            }

            if (possible_moves.Count == 0)
            {
                return [-1, -1];
            }
            if (possible_moves.Count == 1)
            {
                return possible_moves[0];
            }
            Random rnd = new Random();
            int idx = rnd.Next(0, possible_moves.Count);
           return possible_moves[idx];
        }

        private PlayerConnection GetPlayer(Guid id) => id == Player1.Id ? Player1 : Player2;
        private PlayerConnection GetOpponent(Guid id) => id == Player1.Id ? Player2 : Player1;
    }

    public class Ship
    {
        public int X { get; }
        public int Y { get; }
        public int Len { get; }
        public bool Horizontal { get; }

        public Ship(int x, int y, int len, bool horizontal)
        {
            X = x; Y = y; Len = len; Horizontal = horizontal;
        }


            var turnPayload = JsonSerializer.SerializeToElement(new { current = CurrentPlayerId.ToString() });
            await Player1.Conn.SendAsync(new MessageDto { Type = "turn", Payload = turnPayload });
            await Player2.Conn.SendAsync(new MessageDto { Type = "turn", Payload = turnPayload });
        }

        private PlayerConnection GetConn(Guid id) => id == Player1.Id ? Player1.Conn : Player2.Conn;
    }
}
