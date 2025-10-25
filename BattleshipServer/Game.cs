using BattleshipServer.Data;
using BattleshipServer.Domain;
using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipServer.GameFacade;

namespace BattleshipServer
{
    public interface IClonable
    {
        Game Clone();
    }

    public class Game : IClonable
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
        private readonly GameFacade.GameFacade gameFacade;

        public bool IsReady => _ships1.Count > 0 && _ships2.Count > 0;
        public bool GameModesMatch => isStandartGame1 == isStandartGame2;

        private bool _isGameOver = false;


        public event Action<Guid, int, int, bool, bool, List<(int x, int y)>>? ShotResolved;


        public Game(PlayerConnection p1, PlayerConnection p2, GameManager manager, Database db)
        {
            Player1 = p1;
            Player2 = p2;
            _manager = manager;
            _db = db;
            gameFacade = new GameFacade.GameFacade(manager, db);
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
            if (playerId == Player1.Id) isStandartGame1 = isStandartGameVal;
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
            await gameFacade.HandleShot(this, shooterId, x, y, isDoubleBomb);
        }

        public void SetCurrentPlayer(Guid playerId)
        {
            if (playerId != Player1.Id && playerId != Player2.Id)
                throw new ArgumentException("Invalid playerId");
            CurrentPlayerId = playerId;
        }

        public (int[,], List<Ship>) GetBoard1AndShips()
        {
            return (_board1, _ships1);
        }

        public (int[,], List<Ship>) GetBoard2AndShips()
        {
            return (_board2, _ships2);
        }

        public bool GetIsGameOver()
        {
            return _isGameOver;
        }

        public void SetIsGameOver(bool val)
        {
            _isGameOver = val;
        }

        public void InvokeShotResolved(Guid shooterId, int x, int y, bool hit, bool wholeDown, List<(int x, int y)> sunkCells)
        {
            ShotResolved?.Invoke(shooterId, x, y, hit, wholeDown, sunkCells ?? new List<(int, int)>());
        }

        public Game Clone()
        {
            var clone = (Game)MemberwiseClone();
            var board1Copy = (int[,])_board1.Clone();
            var board2Copy = (int[,])_board2.Clone();

            var ships1Copy = _ships1.Select(s => s.Clone()).ToList();
            var ships2Copy = _ships2.Select(s => s.Clone()).ToList();

            typeof(Game).GetField("_board1", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(clone, board1Copy);
            typeof(Game).GetField("_board2", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(clone, board2Copy);
            typeof(Game).GetField("_ships1", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(clone, ships1Copy);
            typeof(Game).GetField("_ships2", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(clone, ships2Copy);
            return clone;
        }

        public List<ShipDto> GetPlayerShips(Guid id)
        {
            var ships = id == Player1.Id ? _ships1 : _ships2;
            var shipsDto = ships.Select(s => new ShipDto
            {
                X = s.X,
                Y = s.Y,
                Len = s.Len,
                Dir = s.Horizontal ? "H" : "V"
            }).ToList();
            return shipsDto;
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
                    if (board[cy, cx] != 3 && board[cy, cx] != 4) return false;
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

            public Ship Clone()
            {
                return new Ship(X, Y, Len, Horizontal);
            }
        }
    }
}
