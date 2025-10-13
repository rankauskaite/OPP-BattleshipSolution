using BattleshipClient.Models;

namespace BattleshipClient.Services
{
    class MessageService
    {
        public MessageService()
        {
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
                    if (dto.Payload.TryGetProperty("current", out var cur))
                        form.isMyTurn = cur.GetString() == form.myId;
                    form.lblStatus.Text = $"Game started. Opponent: {dto.Payload.GetProperty("opponent").GetString()}. Your turn: {form.isMyTurn}";
                    SoundFactory.Play(MusicType.GameStart);
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

                    if (shooter == form.myId)
                        form.enemyBoard.SetCell(x, y, res == "hit" ? CellState.Hit : res == "whole_ship_down" ? CellState.Whole_ship_down : CellState.Miss);
                    else
                        form.ownBoard.SetCell(x, y, res == "hit" ? CellState.Hit : res == "whole_ship_down" ? CellState.Whole_ship_down : CellState.Miss);

                    form.lblStatus.Text = $"Shot result: {res} at {x},{y}";
                    if (res == "hit")
                        SoundFactory.Play(HitType.Hit);
                    else if (res == "whole_ship_down")
                        SoundFactory.Play(HitType.Explosion);
                    else
                        SoundFactory.Play(HitType.Miss);

                    break;

                case "gameOver":
                    if (dto.Payload.TryGetProperty("winnerId", out var w))
                    {
                        var winner = w.GetString();
                        form.lblStatus.Text = winner == form.myId ? "You WON! Game over." : "You lost. Game over.";
                        MessageBox.Show(form.lblStatus.Text, "Game Over");
                        SoundFactory.StopBackground();
                        SoundFactory.Play(MusicType.GameEnd);
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
