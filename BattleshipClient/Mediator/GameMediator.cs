using System;
using System.Threading.Tasks;

namespace BattleshipClient.Mediator
{
    public sealed class GameMediator : IGameMediator
    {
        private readonly MainForm _form;
        private readonly IShotSender _sender;
        private readonly IPowerUpContext _powerUps;

        public GameMediator(MainForm form, IShotSender sender, IPowerUpContext powerUps)
        {
            _form = form;
            _sender = sender;
            _powerUps = powerUps;
        }

        public async Task RequestShotAsync(int x, int y)
        {
            if (!_form.isMyTurn)
            {
                _form.lblStatus.Text = "Not your turn.";
                return;
            }

            _form.lblStatus.Text = $"Firing at {x},{y}...";

            var opts = _powerUps.TakeOptionsAndConsume();

            var shot = new
            {
                type = "shot",
                payload = new
                {
                    x = x,
                    y = y,
                    doubleBomb = opts.DoubleBomb,
                    plusShape = opts.PlusShape,
                    xShape = opts.XShape,
                    superDamage = opts.SuperDamage
                }
            };

            try
            {
                await _sender.SendAsync(shot);
            }
            catch (Exception ex)
            {
                _form.lblStatus.Text = "Send failed: " + ex.Message;
            }
        }
    }
}
