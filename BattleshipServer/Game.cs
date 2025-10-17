using BattleshipServer.Data;
using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer
{
    public class Game
    {
        public PlayerConnection Player1 { get; }
        public PlayerConnection Player2 { get; }
        public Guid CurrentPlayerId { get; private set; }

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

        public event Action<Guid,int,int,bool,bool,List<(int x,int y)>>? ShotResolved;


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
                    {
                        board[cy, cx] = 1;
                    }
                }
            }

            // Save map JSON (playerName string used)
            var mapJson = JsonSerializer.Serialize(shipsDto);
            _db.SaveMap(playerId.ToString(), mapJson);
        }

        public void SetGameMode(Guid playerId, bool isStandartGameVal)
        {
            bool isStandartGame = playerId == Player1.Id ? isStandartGame1 : isStandartGame2;
            if(playerId == Player1.Id) isStandartGame1 = isStandartGameVal;
            else isStandartGame2 = isStandartGameVal;
        }

        public async Task StartGame()
        {
            CurrentPlayerId = Player1.Id; // p1 starts

            var p1Payload = JsonSerializer.SerializeToElement(new { opponent = Player2.Name, yourId = Player1.Id.ToString(), opponentId = Player2.Id.ToString(), current = CurrentPlayerId.ToString() });
            var p2Payload = JsonSerializer.SerializeToElement(new { opponent = Player1.Name, yourId = Player2.Id.ToString(), opponentId = Player1.Id.ToString(), current = CurrentPlayerId.ToString() });

            await Player1.SendAsync(new Models.MessageDto { Type = "startGame", Payload = p1Payload });
            await Player2.SendAsync(new Models.MessageDto { Type = "startGame", Payload = p2Payload });

            Console.WriteLine($"[Game] Started: {Player1.Name} vs {Player2.Name}");
        }

        public async Task ProcessShot(Guid shooterId, int x, int y, bool isDoubleBomb)
        {
            if (shooterId != CurrentPlayerId)
            {
                await GetPlayer(shooterId).SendAsync(new Models.MessageDto { Type = "error", Payload = JsonDocument.Parse("{\"message\":\"Not your turn\"}").RootElement });
                return;
            }

            var shooter = GetPlayer(shooterId);
            var target = GetOpponent(shooterId);
            var targetBoard = target == Player1 ? _board1 : _board2;
            var targetShips = target == Player1 ? _ships1 : _ships2;

            bool hit = false;
            bool success = false;
            bool gameOver = false;

            if (x < 0 || x >= 10 || y < 0 || y >= 10)
            {
                await shooter.SendAsync(new Models.MessageDto { Type = "error", Payload = JsonDocument.Parse("{\"message\":\"Invalid coords\"}").RootElement });
                return;
            }

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

            // Update ship sink status and check overall survival
            bool anyLeft = false;
            bool wholeDown = false; 
            List<(int x,int y)> sunkCells = null;
            foreach (var s in targetShips)
            {
                bool is_sunk = s.IsSunk(targetBoard);
                if (!is_sunk)
                {
                    anyLeft = true;
                }
                else
                {
                    s.setAsSunk(targetBoard);
                    int cy = s.Y + (s.Horizontal ? 0 : s.Len);
                    int cx = s.X + (s.Horizontal ? s.Len : 0);
                    wholeDown = true;
                    if (s.Y <= y && y <= cy && s.X <= x && x <= cx)
                    { 
                        sunkCells = new List<(int, int)>();
                        for (int i = 0; i < s.Len; i++)
                        {
                            int cx1 = s.X + (s.Horizontal ? i : 0);
                            int cy1 = s.Y + (s.Horizontal ? 0 : i);
                            if (cx1 < 0 || cx1 >= 10 || cy1 < 0 || cy1 >= 10) break;
                            var updateBoard = JsonSerializer.SerializeToElement(new { x=cx1, y=cy1, result = "whole_ship_down", shooterId = shooterId.ToString(), targetId = target.Id.ToString() });
                            await Player1.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = updateBoard });
                            await Player2.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = updateBoard }); 

                            sunkCells.Add((cx1, cy1));
                        }
                    }
                }
            }
            if (!anyLeft) gameOver = true;

            var shotResult = JsonSerializer.SerializeToElement(new { x, y, result = hit && !wholeDown ? "hit" : hit && wholeDown ? "whole_ship_down" : "miss", shooterId = shooterId.ToString(), targetId = target.Id.ToString() });
            await Player1.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult });
            await Player2.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult }); 

            ShotResolved?.Invoke(shooterId, x, y, hit, wholeDown, sunkCells ?? new List<(int,int)>());


            if (gameOver)
            {
                var winner = shooterId.ToString();
                var goPayload = JsonSerializer.SerializeToElement(new { winnerId = winner });
                await Player1.SendAsync(new Models.MessageDto { Type = "gameOver", Payload = goPayload });
                await Player2.SendAsync(new Models.MessageDto { Type = "gameOver", Payload = goPayload });

                _manager.GameEnded(this);
                _db.SaveGame(Player1.Name ?? Player1.Id.ToString(), Player2.Name ?? Player2.Id.ToString(), shooterId.ToString());
                Console.WriteLine($"[Game] Game over. Winner: {winner}");
            }
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

        public bool IsSunk(int[,] board)
        {
            for (int i = 0; i < Len; i++)
            {
                int cx = X + (Horizontal ? i : 0);
                int cy = Y + (Horizontal ? 0 : i);
                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10) return false;
                if (board[cy, cx] != 3) return false; // not hit
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
        }
    }
}
