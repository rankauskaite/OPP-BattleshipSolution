using BattleshipServer.Data;
using BattleshipServer.Domain;
using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using BattleshipServer.GameFacade;
using BattleshipServer.Defense;
using BattleshipServer.State;
using BattleshipServer.PowerUps;

namespace BattleshipServer
{
    public interface IClonable
    {
        Game Clone();
    }

    public interface IGameMemento
    {
    }

    public class Game : IClonable
    {
        public PlayerConnection Player1 { get; }
        public PlayerConnection Player2 { get; }
        public Guid CurrentPlayerId { get; private set; }

        private readonly int[,] _board1 = new int[10, 10];
        private readonly int[,] _board2 = new int[10, 10];

        private readonly DefenseComposite _defense1 = new DefenseComposite();
        private readonly DefenseComposite _defense2 = new DefenseComposite();
        private bool isStandartGame1 = true;
        private bool isStandartGame2 = true;
        private readonly List<Ship> _ships1 = new();
        private readonly List<Ship> _ships2 = new();
        private readonly GameManager _manager;
        private readonly Database _db;
        private readonly GameFacade.GameFacade gameFacade = new GameFacade.GameFacade();

        public bool IsReady => _ships1.Count > 0 && _ships2.Count > 0;
        public bool GameModesMatch => isStandartGame1 == isStandartGame2;

        private bool _isGameOver = false;

        private int _healUsedP1, _healUsedP2;
        private int _safeShieldsUsedP1, _safeShieldsUsedP2;
        private int _invisibleShieldsUsedP1, _invisibleShieldsUsedP2;
        private int _plusUsedP1, _plusUsedP2;
        private int _xUsedP1, _xUsedP2;
        private int _superUsedP1, _superUsedP2;
        private int _doubleBombUsedP1, _doubleBombUsedP2;

        public int HealUsedP1 => _healUsedP1;
        public int HealUsedP2 => _healUsedP2;
        public int SafeShieldsUsedP1 => _safeShieldsUsedP1;
        public int SafeShieldsUsedP2 => _safeShieldsUsedP2;
        public int InvisibleShieldsUsedP1 => _invisibleShieldsUsedP1;
        public int InvisibleShieldsUsedP2 => _invisibleShieldsUsedP2;

        public int PlusUsedP1 => _plusUsedP1;
        public int PlusUsedP2 => _plusUsedP2;
        public int XUsedP1 => _xUsedP1;
        public int XUsedP2 => _xUsedP2;
        public int SuperUsedP1 => _superUsedP1;
        public int SuperUsedP2 => _superUsedP2;
        public int DoubleBombUsedP1 => _doubleBombUsedP1;
        public int DoubleBombUsedP2 => _doubleBombUsedP2;

        private sealed class GameMemento : IGameMemento
        {
            public int[,] Board1 { get; }
            public int[,] Board2 { get; }
            public List<Ship> Ships1 { get; }
            public List<Ship> Ships2 { get; }

            public int CurrentPlayerIndex { get; }

            public bool IsStandardGame1 { get; }
            public bool IsStandardGame2 { get; }
            public bool IsGameOver { get; }

            public int HealUsedP1 { get; }
            public int HealUsedP2 { get; }
            public int SafeShieldsUsedP1 { get; }
            public int SafeShieldsUsedP2 { get; }
            public int InvisibleShieldsUsedP1 { get; }
            public int InvisibleShieldsUsedP2 { get; }

            public int PlusUsedP1 { get; }
            public int PlusUsedP2 { get; }
            public int XUsedP1 { get; }
            public int XUsedP2 { get; }
            public int SuperUsedP1 { get; }
            public int SuperUsedP2 { get; }
            public int DoubleBombUsedP1 { get; }
            public int DoubleBombUsedP2 { get; }

            public GameMemento(
                int[,] board1,
                int[,] board2,
                List<Ship> ships1,
                List<Ship> ships2,
                int currentPlayerIndex,
                bool isStandardGame1,
                bool isStandardGame2,
                bool isGameOver,
                int healUsedP1,
                int healUsedP2,
                int safeShieldsUsedP1,
                int safeShieldsUsedP2,
                int invisibleShieldsUsedP1,
                int invisibleShieldsUsedP2,
                int plusUsedP1,
                int plusUsedP2,
                int xUsedP1,
                int xUsedP2,
                int superUsedP1,
                int superUsedP2,
                int doubleBombUsedP1,
                int doubleBombUsedP2)
            {
                Board1 = board1;
                Board2 = board2;
                Ships1 = ships1;
                Ships2 = ships2;
                CurrentPlayerIndex = currentPlayerIndex;
                IsStandardGame1 = isStandardGame1;
                IsStandardGame2 = isStandardGame2;
                IsGameOver = isGameOver;

                HealUsedP1 = healUsedP1;
                HealUsedP2 = healUsedP2;
                SafeShieldsUsedP1 = safeShieldsUsedP1;
                SafeShieldsUsedP2 = safeShieldsUsedP2;
                InvisibleShieldsUsedP1 = invisibleShieldsUsedP1;
                InvisibleShieldsUsedP2 = invisibleShieldsUsedP2;

                PlusUsedP1 = plusUsedP1;
                PlusUsedP2 = plusUsedP2;
                XUsedP1 = xUsedP1;
                XUsedP2 = xUsedP2;
                SuperUsedP1 = superUsedP1;
                SuperUsedP2 = superUsedP2;
                DoubleBombUsedP1 = doubleBombUsedP1;
                DoubleBombUsedP2 = doubleBombUsedP2;
            }
        }

        public event Action<Guid, int, int, bool, bool, List<(int x, int y)>>? ShotResolved;

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

            var mapJson = JsonSerializer.Serialize(shipsDto);
            _db.SaveMap(playerId.ToString(), mapJson);
        }

        public void SetGameMode(Guid playerId, bool isStandartGameVal)
        {
            if (playerId == Player1.Id) isStandartGame1 = isStandartGameVal;
            else if (playerId == Player2.Id) isStandartGame2 = isStandartGameVal;
            else throw new ArgumentException("Invalid playerId");
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

            await Player1.SendAsync(new Models.MessageDto { Type = "startGame", Payload = p1Payload });
            await Player2.SendAsync(new Models.MessageDto { Type = "startGame", Payload = p2Payload });

            Console.WriteLine($"[Game] Started: {Player1.Name} vs {Player2.Name}");
        }

        public async Task ProcessShot(Guid shooterId, int x, int y, bool isDoubleBomb)
        {
            await gameFacade.HandleShot(this, shooterId, x, y, isDoubleBomb);
        }

        public async Task ProcessCompositeShot(Guid shooterId, int x0, int y0, bool isDoubleBomb, bool plusShape, bool xShape, bool superDamage)
        {
            await gameFacade.HandleCompositeShot(this, shooterId, x0, y0, isDoubleBomb, plusShape, xShape, superDamage);
        }

        public void RegisterPowerUpUse(Guid shooterId, bool isDoubleBomb, bool plusShape, bool xShape, bool superDamage)
        {
            bool isP1 = shooterId == Player1.Id;
            bool isP2 = shooterId == Player2.Id;
            if (!isP1 && !isP2) return;

            if (plusShape)
            {
                if (isP1) _plusUsedP1++;
                else _plusUsedP2++;
            }

            if (xShape)
            {
                if (isP1) _xUsedP1++;
                else _xUsedP2++;
            }

            if (superDamage)
            {
                if (isP1) _superUsedP1++;
                else _superUsedP2++;
            }

            if (isDoubleBomb)
            {
                if (isP1) _doubleBombUsedP1++;
                else _doubleBombUsedP2++;
            }
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

        public DefenseComposite GetDefenseForPlayer(Guid playerId)
        {
            if (playerId == Player1.Id) return _defense1;
            if (playerId == Player2.Id) return _defense2;
            throw new ArgumentException("Invalid playerId", nameof(playerId));
        }

        public void AddDefense(Guid playerId, IDefenseComponent component)
        {
            if (playerId == Player1.Id)
                _defense1.Add(component);
            else if (playerId == Player2.Id)
                _defense2.Add(component);
            else
                throw new ArgumentException("Invalid playerId", nameof(playerId));
        }

        public void AddCellShield(Guid playerId, int x, int y, DefenseMode mode)
        {
            AddDefense(playerId, new CellShield(x, y, mode));

            if (mode == DefenseMode.Safetiness)
            {
                if (playerId == Player1.Id) _safeShieldsUsedP1++;
                else if (playerId == Player2.Id) _safeShieldsUsedP2++;
            }
            else if (mode == DefenseMode.Visibility)
            {
                if (playerId == Player1.Id) _invisibleShieldsUsedP1++;
                else if (playerId == Player2.Id) _invisibleShieldsUsedP2++;
            }
        }

        public void AddAreaShield(Guid playerId, int x1, int y1, int x2, int y2, DefenseMode mode)
        {
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);

            var areaComposite = new DefenseComposite();

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x < 0 || x > 9 || y < 0 || y > 9)
                        continue;

                    areaComposite.Add(new CellShield(x, y, mode));
                }
            }

            AddDefense(playerId, areaComposite);

            if (mode == DefenseMode.Safetiness)
            {
                if (playerId == Player1.Id) _safeShieldsUsedP1++;
                else if (playerId == Player2.Id) _safeShieldsUsedP2++;
            }
            else if (mode == DefenseMode.Visibility)
            {
                if (playerId == Player1.Id) _invisibleShieldsUsedP1++;
                else if (playerId == Player2.Id) _invisibleShieldsUsedP2++;
            }
        }

        public List<(int x, int y)> HealShip(Guid playerId, int x, int y)
        {
            int[,] board;
            List<Ship> ships;

            if (playerId == Player1.Id)
            {
                board = _board1;
                ships = _ships1;
            }
            else if (playerId == Player2.Id)
            {
                board = _board2;
                ships = _ships2;
            }
            else
            {
                return new List<(int, int)>();
            }

            Ship? ship = ships.FirstOrDefault(s => s.Contains(x, y));
            if (ship == null)
            {
                return new List<(int, int)>();
            }

            if (!ship.IsDamagedButNotSunk(board))
            {
                return new List<(int, int)>();
            }

            var healedCells = new List<(int x, int y)>();

            for (int i = 0; i < ship.Len; i++)
            {
                int cx = ship.Horizontal ? ship.X + i : ship.X;
                int cy = ship.Horizontal ? ship.Y : ship.Y + i;

                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10)
                    continue;

                if (board[cy, cx] == 3)
                {
                    board[cy, cx] = 1;
                    healedCells.Add((cx, cy));
                }
            }

            if (healedCells.Count > 0)
            {
                if (playerId == Player1.Id) _healUsedP1++;
                else if (playerId == Player2.Id) _healUsedP2++;
            }

            return healedCells;
        }

        public bool GetIsGameOver()
        {
            return _isGameOver;
        }

        public void SetIsGameOver(bool val)
        {
            _isGameOver = val;
            if (val)
            {
                _manager.GameEnded(this);
            }
        }

        public void InvokeShotResolved(Guid shooterId, int x, int y, bool hit, bool wholeDown, List<(int x, int y)> sunkCells)
        {
            ShotResolved?.Invoke(shooterId, x, y, hit, wholeDown, sunkCells ?? new List<(int, int)>());
        }

        public void SaveGameToDB(Guid shooterId)
        {
            _db.SaveGame(Player1.Name ?? Player1.Id.ToString(), Player2.Name ?? Player2.Id.ToString(), shooterId.ToString());
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

        private int SumBoard(int[,] board)
        {
            int sum = 0;
            foreach (var cell in board) sum += cell;
            return sum;
        }

        public IGameMemento CreateMemento()
        {
            var board1Copy = (int[,])_board1.Clone();
            var board2Copy = (int[,])_board2.Clone();

            var ships1Copy = _ships1.Select(s => s.Clone()).ToList();
            var ships2Copy = _ships2.Select(s => s.Clone()).ToList();

            int currentIdx =
                CurrentPlayerId == Player1.Id ? 1 :
                CurrentPlayerId == Player2.Id ? 2 : 0;

            var memento = new GameMemento(
                board1Copy,
                board2Copy,
                ships1Copy,
                ships2Copy,
                currentIdx,
                isStandartGame1,
                isStandartGame2,
                _isGameOver,
                _healUsedP1,
                _healUsedP2,
                _safeShieldsUsedP1,
                _safeShieldsUsedP2,
                _invisibleShieldsUsedP1,
                _invisibleShieldsUsedP2,
                _plusUsedP1,
                _plusUsedP2,
                _xUsedP1,
                _xUsedP2,
                _superUsedP1,
                _superUsedP2,
                _doubleBombUsedP1,
                _doubleBombUsedP2
            );

            Console.WriteLine($"[MEMENTO] CreateMemento: P1sum={SumBoard(board1Copy)}, P2sum={SumBoard(board2Copy)}, currentIdx={currentIdx}, isGameOver={_isGameOver}");
            return memento;
        }

        public void RestoreMemento(IGameMemento memento)
        {
            if (memento is not GameMemento gm)
                throw new ArgumentException("Invalid memento instance", nameof(memento));

            CopyFrom(gm);
        }

        private void CopyFrom(GameMemento state)
        {
            Array.Copy(state.Board1, _board1, _board1.Length);
            Array.Copy(state.Board2, _board2, _board2.Length);

            _ships1.Clear();
            _ships1.AddRange(state.Ships1.Select(s => s.Clone()));
            _ships2.Clear();
            _ships2.AddRange(state.Ships2.Select(s => s.Clone()));

            isStandartGame1 = state.IsStandardGame1;
            isStandartGame2 = state.IsStandardGame2;

            if (state.CurrentPlayerIndex == 1)
                CurrentPlayerId = Player1.Id;
            else if (state.CurrentPlayerIndex == 2)
                CurrentPlayerId = Player2.Id;
            else
                CurrentPlayerId = Player1.Id;

            _isGameOver = state.IsGameOver;

            _healUsedP1 = state.HealUsedP1;
            _healUsedP2 = state.HealUsedP2;
            _safeShieldsUsedP1 = state.SafeShieldsUsedP1;
            _safeShieldsUsedP2 = state.SafeShieldsUsedP2;
            _invisibleShieldsUsedP1 = state.InvisibleShieldsUsedP1;
            _invisibleShieldsUsedP2 = state.InvisibleShieldsUsedP2;

            _plusUsedP1 = state.PlusUsedP1;
            _plusUsedP2 = state.PlusUsedP2;
            _xUsedP1 = state.XUsedP1;
            _xUsedP2 = state.XUsedP2;
            _superUsedP1 = state.SuperUsedP1;
            _superUsedP2 = state.SuperUsedP2;
            _doubleBombUsedP1 = state.DoubleBombUsedP1;
            _doubleBombUsedP2 = state.DoubleBombUsedP2;

            Console.WriteLine($"[MEMENTO] RestoreMemento: P1sum={SumBoard(_board1)}, P2sum={SumBoard(_board2)}, currentIdx={state.CurrentPlayerIndex}, current={CurrentPlayerId}, isGameOver={_isGameOver}");
        }

        public void ReplacePlayerConnection(PlayerConnection newConnection)
        {
            if (Player1.Name == newConnection.Name)
            {
                var field = typeof(Game).GetField("<Player1>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(this, newConnection);
                Console.WriteLine($"[Reconnect] Player1 re-bound to {newConnection.Name} ({newConnection.Id})");
            }
            else if (Player2.Name == newConnection.Name)
            {
                var field = typeof(Game).GetField("<Player2>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(this, newConnection);
                Console.WriteLine($"[Reconnect] Player2 re-bound to {newConnection.Name} ({newConnection.Id})");
            }
            else
            {
                Console.WriteLine($"[Reconnect] Player name {newConnection.Name} not found in this game.");
            }
        }

        public int[][] GetBoardForPlayerAsJagged(Guid playerId)
        {
            var board = playerId == Player1.Id ? _board1 : _board2;
            int sizeY = board.GetLength(0);
            int sizeX = board.GetLength(1);
            var result = new int[sizeY][];
            for (int y = 0; y < sizeY; y++)
            {
                result[y] = new int[sizeX];
                for (int x = 0; x < sizeX; x++)
                {
                    result[y][x] = board[y, x];
                }
            }
            return result;
        }

        public int[][] GetEnemyBoardViewForPlayerAsJagged(Guid playerId)
        {
            var enemy = playerId == Player1.Id ? _board2 : _board1;
            int sizeY = enemy.GetLength(0);
            int sizeX = enemy.GetLength(1);
            var result = new int[sizeY][];
            for (int y = 0; y < sizeY; y++)
            {
                result[y] = new int[sizeX];
                for (int x = 0; x < sizeX; x++)
                {
                    int cell = enemy[y, x];
                    result[y][x] = (cell == 1) ? 0 : cell;
                }
            }
            return result;
        }

        public bool IsStandardForPlayer(Guid playerId)
        {
            return playerId == Player1.Id ? isStandartGame1 : isStandartGame2;
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
                X = x;
                Y = y;
                Len = len;
                Horizontal = horizontal;
            }

            public bool Contains(int x, int y)
            {
                if (Horizontal)
                {
                    return y == Y && x >= X && x < X + Len;
                }
                else
                {
                    return x == X && y >= Y && y < Y + Len;
                }
            }

            public bool IsSunk(int[,] board)
            {
                for (int i = 0; i < Len; i++)
                {
                    int cx = X + (Horizontal ? i : 0);
                    int cy = Y + (Horizontal ? 0 : i);

                    if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10)
                        return false;

                    int cell = board[cy, cx];
                    if (cell != 3 && cell != 4)
                        return false;
                }
                return true;
            }

            public void setAsSunk(int[,] board)
            {
                for (int i = 0; i < Len; i++)
                {
                    int cx = X + (Horizontal ? i : 0);
                    int cy = Y + (Horizontal ? 0 : i);

                    if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10)
                        return;

                    board[cy, cx] = 4;
                }
            }

            public bool IsDamagedButNotSunk(int[,] board)
            {
                bool hasHit = false;

                for (int i = 0; i < Len; i++)
                {
                    int cx = X + (Horizontal ? i : 0);
                    int cy = Y + (Horizontal ? 0 : i);

                    if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10)
                        continue;

                    if (board[cy, cx] == 3)
                    {
                        hasHit = true;
                    }
                }

                return hasHit && !IsSunk(board);
            }

            public void Heal(int[,] board)
            {
                for (int i = 0; i < Len; i++)
                {
                    int cx = X + (Horizontal ? i : 0);
                    int cy = Y + (Horizontal ? 0 : i);

                    if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10)
                        continue;

                    if (board[cy, cx] == 3)
                    {
                        board[cy, cx] = 1;
                    }
                }
            }

            public Ship Clone()
            {
                return new Ship(X, Y, Len, Horizontal);
            }
        }
    }
}