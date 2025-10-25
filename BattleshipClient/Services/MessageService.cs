using BattleshipClient.Models;
using BattleshipClient.Observers;
using System.Text.Json;

namespace BattleshipClient.Services
{
    class MessageService
    {
        private readonly GameEventManager _eventManager = new();
        public MessageService()
        {
            _eventManager.Attach(new SoundObserver());
            _eventManager.Attach(new LoggerObserver());
        }

        public void HandleMessage(MessageDto dto, MainForm form)
        {
            switch (dto.Type)
            {
                case "info":
                    if (dto.Payload.TryGetProperty("message", out var me))
                        form.lblStatus.Text = me.GetString(); //tik i viena puse
                    break;

                case "startGame":
                    if (dto.Payload.TryGetProperty("yourId", out var yi)) form.myId = yi.GetString();
                    if (dto.Payload.TryGetProperty("opponentId", out var oi)) form.oppId = oi.GetString();
                    if (dto.Payload.TryGetProperty("opponent", out var on)) form.oppName = on.GetString();
                    if (dto.Payload.TryGetProperty("current", out var cur))
                        form.isMyTurn = cur.GetString() == form.myId;
                    form.lblStatus.Text = $"Game started. Opponent: {dto.Payload.GetProperty("opponent").GetString()}. Your turn: {form.isMyTurn}";
                    SoundFactory.Play(MusicType.GameStart);
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
                    int x = dto.Payload.GetProperty("x").GetInt32();
                    int y = dto.Payload.GetProperty("y").GetInt32();
                    string res = dto.Payload.GetProperty("result").GetString();
                    string shooter = dto.Payload.GetProperty("shooterId").GetString();

                    bool isMyShot = shooter == form.myId;
                    string shooterName = isMyShot ? form.myName : form.oppName;

                    if (shooter == form.myId)
                        form.enemyBoard.SetCell(x, y, res == "hit" ? CellState.Hit : res == "whole_ship_down" ? CellState.Whole_ship_down : CellState.Miss);
                    else
                        form.ownBoard.SetCell(x, y, res == "hit" ? CellState.Hit : res == "whole_ship_down" ? CellState.Whole_ship_down : CellState.Miss);

                    form.lblStatus.Text = $"Shot result: {res} at {x},{y}";
                    if (res == "hit")
                        _eventManager.Notify("HIT", shooterName);
                    else if (res == "whole_ship_down")
                        _eventManager.Notify("EXPLOSION", shooterName);
                    else
                        _eventManager.Notify("MISS", shooterName);

                    break;

                case "gameOver":
                    if (dto.Payload.TryGetProperty("winnerId", out var w))
                    {
                        var winner = w.GetString();
                        string winnerName = winner == form.myId ? form.myName : form.oppName;
                        form.lblStatus.Text = winner == form.myId ? "You WON! Game over." : "You lost. Game over.";
                        MessageBox.Show(form.lblStatus.Text, "Game Over");
                        _eventManager.Notify(winner == form.myId ? "WIN" : "LOSE", winnerName);
                        //SoundFactory.Play(MusicType.GameEnd);
                        form.btnGameOver.Visible = true;
                        form.isMyTurn = false;
                    }
                    break;

                case "error":
                    if (dto.Payload.TryGetProperty("message", out var err))
                        MessageBox.Show(err.GetString(), "Error");
                    break;
            }
        }
    }
}
