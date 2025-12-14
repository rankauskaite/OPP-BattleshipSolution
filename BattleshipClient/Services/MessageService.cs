using BattleshipClient.Models;
using BattleshipClient.Observers;
using BattleshipClient.Factory;
using System.Windows.Forms;
using System.Text.Json;
using BattleshipClient.Services;
using BattleshipClient.Services.MessageHandlers;

namespace BattleshipClient.Services
{
    public class MessageService
    {
        private readonly GameEventManager _eventManager = new();
        private readonly SoundService _soundService;
        private readonly string _localPlayerName;
        private readonly IClientMessageHandler _handlerChain;


        public MessageService(string localPlayerName)
        {
            _localPlayerName = localPlayerName;
            _soundService = new SoundService(new SoundFactory());
            _eventManager.Attach(new SoundObserver(_soundService));
            _eventManager.Attach(new LoggerObserver(_localPlayerName));

            // Chain of Responsibility grandinė (5 elementai)
            _handlerChain = new PowerUpSummaryHandler(this);
            _handlerChain
                .SetNext(new ShotMessageHandler(this))
                .SetNext(new TurnMessageHandler(this))
                .SetNext(new GameOverMessageHandler(this))
                .SetNext(new LegacyMessageHandler(this));
        }

        public void HandleMessage(MessageDto dto, MainForm form)
        {
            _handlerChain.Handle(dto, form);
        }
        internal void HandlePowerUpSummary(MessageDto dto, MainForm form)
        {
            string powerUp = dto.Payload.GetProperty("powerUp").GetString() ?? "PowerUp";

            var hits = dto.Payload.GetProperty("hits")
                .EnumerateArray()
                .Select(h => new System.Drawing.Point(
                    h.GetProperty("x").GetInt32(),
                    h.GetProperty("y").GetInt32()))
                .ToList();

            string Format(System.Drawing.Point p) => $"{(char)('A' + p.X)}{p.Y + 1}";
            string msg = hits.Count == 0
                ? $"{powerUp} power-up: nepataikė."
                : $"{powerUp} power-up pataikė į: {string.Join(", ", hits.Select(Format))}";

            System.Windows.Forms.MessageBox.Show(
                msg,
                "Power-up rezultatas",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information
            );
        }

        internal void HandleShotMessage(MessageDto dto, MainForm form)
        {
            if (dto.Type != "shotResult") return;

            int x = dto.Payload.GetProperty("x").GetInt32();
            int y = dto.Payload.GetProperty("y").GetInt32();
            string res = dto.Payload.GetProperty("result").GetString();
            string shooter = dto.Payload.GetProperty("shooterId").GetString();

            bool isMyShot = shooter == form.myId;
            string shooterName = isMyShot ? form.myName : form.oppName;

            var board = isMyShot ? form.enemyBoard : form.ownBoard;
            var prevState = board.GetCell(x, y);

            CellState newState = res switch
            {
                "hit" => CellState.Hit,
                "whole_ship_down" => CellState.Whole_ship_down,
                "shield" => CellState.Shielded,
                _ => CellState.Miss
            };

            var cmd = new BattleshipClient.Commands.ShotCommand(
                board, x, y, prevState, newState, shooterName);
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
                case "shield":
                    _eventManager.Notify("MISS", shooterName);
                    break;
                default:
                    _eventManager.Notify("MISS", shooterName);
                    break;
            }
        }

        internal void HandleTurn(MessageDto dto, MainForm form)
        {
            if (dto.Payload.TryGetProperty("current", out var cur2))
            {
                form.isMyTurn = cur2.GetString() == form.myId;
                form.lblStatus.Text = form.isMyTurn ? "Your turn" : "Opponent's turn";
            }
        }

        internal void HandleGameOver(MessageDto dto, MainForm form)
        {
            if (dto.Payload.TryGetProperty("winnerId", out var w))
            {
                var winner = w.GetString();
                string winnerName = winner == form.myId ? form.myName : form.oppName;
                form.lblStatus.Text = winner == form.myId ? "You WON! Game over." : "You lost. Game over.";
                MessageBox.Show(form.lblStatus.Text, "Game Over");

                _eventManager.Notify(winner == form.myId ? "WIN" : "LOSE", winnerName);
                _soundService.PlayMusic(MusicType.GameEnd);

                form.btnGameOver.Visible = true;
                form.btnReplay.Visible = true;
                form.isMyTurn = false;
            }
        }

        internal void LegacyHandle(MessageDto dto, MainForm form)
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
                    _soundService.PlayMusic(MusicType.GameStart);
                    break;

                case "shipInfo":
                    if (dto.Payload.TryGetProperty("message", out var me1))
                        form.lblStatus.Text = me1.GetString();
                    if (dto.Payload.TryGetProperty("ships", out var shipsJson))
                    {
                        var ships = JsonSerializer.Deserialize<List<ShipDto>>(shipsJson.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (ships == null) break;

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

                case "healApplied":
                    HandleHealApplied(dto.Payload, form);
                    break;

                case "error":
                    if (dto.Payload.TryGetProperty("message", out var err))
                        MessageBox.Show(err.GetString(), "Error");
                    break;

                case "scoreUpdate":
                    form.UpdateScoreboardUI(dto.Payload);
                    break;

                default:
                    break;
            }
        }

        private void HandleHealApplied(JsonElement payload, MainForm form)
        {
            string healedPlayerId = payload.GetProperty("healedPlayerId").GetString();

            var cells = payload
                .GetProperty("cells")
                .EnumerateArray()
                .Select(c => new System.Drawing.Point(
                    c.GetProperty("x").GetInt32(),
                    c.GetProperty("y").GetInt32()))
                .ToList();

            if (healedPlayerId == form.myId)
            {
                // Mano laivas buvo pagydytas:
                // mano lentoje Hit -> Ship (jei dar nesusitvarkė lokaliai)
                foreach (var cell in cells)
                {
                    var state = form.ownBoard.GetCell(cell.X, cell.Y);
                    if (state == CellState.Hit)
                        form.ownBoard.SetCell(cell.X, cell.Y, CellState.Ship);
                }
                form.ownBoard.Invalidate();
                form.lblStatus.Text = "Tavo laivas buvo pagydytas.";
            }
            else
            {
                // Priešininko laivas pagydytas:
                // mano enemyBoard'e tie Hit turi „išsivalyti“ (vėl tapti nešaudytais / vandeniu)
                foreach (var cell in cells)
                {
                    var state = form.enemyBoard.GetCell(cell.X, cell.Y);
                    if (state == CellState.Hit)
                    {
                        // Čia panaudok tą būseną, kurią naudoji neutraliam langeliui:
                        // jei turi CellState.Empty ar CellState.Unknown – dėk ją
                        form.enemyBoard.SetCell(cell.X, cell.Y, CellState.Empty);
                    }
                }
                form.enemyBoard.Invalidate();
                form.lblStatus.Text = "Priešininkas pagydė savo laivą – keli tavo pataikymai dingo.";
            }
        }
    }
}