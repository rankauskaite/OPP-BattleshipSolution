using BattleshipClient.Models;
using BattleshipClient.Observers;
using BattleshipClient.Factory;
using System.Windows.Forms;
using System.Text.Json;

namespace BattleshipClient.Services
{
    class MessageService
    {
        private readonly GameEventManager _eventManager = new();
        private readonly SoundFactory _factory = new SoundFactory();
        private readonly string _localPlayerName;

        public MessageService(string localPlayerName)
        {
            _localPlayerName = localPlayerName;
            _eventManager.Attach(new SoundObserver());
            _eventManager.Attach(new LoggerObserver(_localPlayerName));
        }

        public void HandleMessage(MessageDto dto, MainForm form)
        {
            switch (dto.Type)
            {
                case "info":
                    if (dto.Payload.TryGetProperty("message", out var me))
                        form.lblStatus.Text = me.GetString();
                    break;

                case "startGame":
                    if (dto.Payload.TryGetProperty("yourId", out var yi)) form.myId = yi.GetString();
                    if (dto.Payload.TryGetProperty("opponentId", out var oi)) form.oppId = oi.GetString();
                    if (dto.Payload.TryGetProperty("opponent", out var on)) form.oppName = on.GetString();
                    if (dto.Payload.TryGetProperty("current", out var cur))
                        form.isMyTurn = cur.GetString() == form.myId;
                    form.lblStatus.Text = $"Game started. Opponent: {dto.Payload.GetProperty("opponent").GetString()}. Your turn: {form.isMyTurn}";
                    _factory.Play(_factory.factoryMethod(MusicType.GameStart));
                    break;
                case "shipInfo":
                    if (dto.Payload.TryGetProperty("message", out var me1))
                        form.lblStatus.Text = me1.GetString();
                    if (dto.Payload.TryGetProperty("ships", out var shipsJson))
                    {
                        var ships = JsonSerializer.Deserialize<List<ShipDto>>(shipsJson.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (ships == null)
                        {
                            break;
                        }
                        var gameService = new GameService();
                        gameService.ResetMyFormOnly(form, true, true, true);
                        foreach (var ship in ships)
                        {
                            bool horiz = ship.dir == "H";
                            ShipPlacementService.PlaceShip(form.ownBoard, ship.x, ship.y, ship.len, horiz);
                            form.myShips.Add(ship);
                        }
                        form.ownBoard.Ships = form.myShips;
                        form.ownBoard.Invalidate();
                        form.btnReady.Enabled = form.myShips.Count == form.GetShipCount();
                    }
                    break;
                case "turn":
                    if (dto.Payload.TryGetProperty("current", out var cur2))
                    {
                        form.isMyTurn = cur2.GetString() == form.myId;
                        form.lblStatus.Text = form.isMyTurn ? "Your turn" : "Opponent's turn";
                    }
                    break;

                case "shotResult":
                    {
                        int x = dto.Payload.GetProperty("x").GetInt32();
                        int y = dto.Payload.GetProperty("y").GetInt32();
                        string res = dto.Payload.GetProperty("result").GetString();
                        string shooter = dto.Payload.GetProperty("shooterId").GetString();

                        bool isMyShot = shooter == form.myId;
                        string shooterName = isMyShot ? form.myName : form.oppName;

                        CellState newState = res switch
                        {
                            "hit" => CellState.Hit,
                            "whole_ship_down" => CellState.Whole_ship_down,
                            _ => CellState.Miss
                        };

                        var board = isMyShot ? form.enemyBoard : form.ownBoard;
                        var prevState = board.GetCell(x, y);

                        var cmd = new BattleshipClient.Commands.ShotCommand(board, x, y, prevState, newState, shooterName);
                        form.CommandManager.ExecuteCommand(cmd);

                        form.lblStatus.Text = $"Shot result: {res} at {x},{y}";

                        switch (res)
                        {
                            case "hit":
                                _eventManager.Notify("HIT", shooterName);
                                break;
                            case "whole_ship_down":
                                _eventManager.Notify("EXPLOSION", shooterName);
                                break;
                            default:
                                _eventManager.Notify("MISS", shooterName);
                                break;
                        }

                        break;
                    }

                case "gameOver":
                    if (dto.Payload.TryGetProperty("winnerId", out var w))
                    {
                        var winner = w.GetString();
                        string winnerName = winner == form.myId ? form.myName : form.oppName;
                        form.lblStatus.Text = winner == form.myId ? "You WON! Game over." : "You lost. Game over.";
                        MessageBox.Show(form.lblStatus.Text, "Game Over");
                        _eventManager.Notify(winner == form.myId ? "WIN" : "LOSE", winnerName);
                        form.btnGameOver.Visible = true;
                        form.btnReplay.Visible = true;
                        form.isMyTurn = false;
                    }
                    break;

                case "error":
                    if (dto.Payload.TryGetProperty("message", out var err))
                        MessageBox.Show(err.GetString(), "Error");
                    break;

                case "scoreUpdate":
                    form.UpdateScoreboardUI(dto.Payload);
                    break;
            }
        }
    }
}