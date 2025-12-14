using System.Drawing;
using System.Windows.Forms;

namespace BattleshipClient.Mediator
{
    public sealed class MainFormPowerUpContext : IPowerUpContext
    {
        private readonly MainForm _form;

        public MainFormPowerUpContext(MainForm form)
        {
            _form = form;
        }

        public ShotOptions TakeOptionsAndConsume()
        {
            var opts = new ShotOptions(
                _form.DoubleBombActive,
                _form.PlusActive,
                _form.XActive,
                _form.SuperActive
            );

            if (_form.PlusActive)
            {
                _form.PlusActive = false;
                _form.PlusUsed = _form.MaxPlusCount;
                _form.BtnPlus.Enabled = false;
                _form.BtnPlus.Text = "+ Shot (used)";
                _form.BtnPlus.BackColor = SystemColors.Control;
            }

            if (_form.XActive)
            {
                _form.XActive = false;
                _form.XUsed = _form.MaxXCount;
                _form.BtnX.Enabled = false;
                _form.BtnX.Text = "X Shot (used)";
                _form.BtnX.BackColor = SystemColors.Control;
            }

            if (_form.SuperActive)
            {
                _form.SuperActive = false;
                _form.SuperUsed = _form.MaxSuperCount;
                _form.BtnSuper.Enabled = false;
                _form.BtnSuper.Text = "Super (used)";
                _form.BtnSuper.BackColor = SystemColors.Control;
            }

            _form.SyncPowerUpsUI();

            if (_form.DoubleBombActive)
            {
                _form.DoubleBombActive = false;
                _form.BtnDoubleBombPowerUp.BackColor = SystemColors.Control;

                _form.DoubleBombsUsed += 1;
                if (_form.DoubleBombsUsed >= _form.MaxDoubleBombsCount)
                {
                    _form.BtnDoubleBombPowerUp.Enabled = false;
                    _form.BtnDoubleBombPowerUp.Visible = false;
                }

                _form.UpdatePowerUpLabel();
            }

            return opts;
        }
    }
}
