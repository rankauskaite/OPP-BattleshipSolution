using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleshipClient.Models;
using BattleshipClient.Services;

namespace BattleshipClient
{
    public class MainForm : Form
    {
        private TextBox txtServer;
        public TextBox txtName { get; private set; }
        private Button btnConnect;
        private Button btnRandomize;
        public Button btnReady;
        private Button btnPlaceShips;
        public Button btnGameOver; // Naujas mygtukas
        public RadioButton radioMiniGame; 
        public Button btnVsBot;
        public RadioButton radioStandartGame;
        public Button btnDoubleBombPowerUp;
        public Label lblStatus;
        public Label lblPowerUpInfo;
        public GameBoard ownBoard { get; set; }
        public GameBoard enemyBoard { get; set; }
        public FlowLayoutPanel shipPanel;

        public NetworkClient net { get; private set; } = new NetworkClient();

        // state
        public List<ShipDto> myShips { get; private set; } = new List<ShipDto>();
        public bool isMyTurn = false;
        public bool doubleBombActive = false;
        public int maxDoubleBombsCount = 0;
        public int doubleBombsUsed = 0;
        public string myId { get; set; } = "";
        public string oppId { get; set; } = "";

        // drag & drop state
        public bool placingShips = false;
        private bool placingHorizontal = true;

        // services
        private ShipPlacementService ShipPlacementService = new ShipPlacementService();
        private GameService GameService = new GameService();
        private MessageService MessageService = new MessageService();

        public List<ShipDto> Ships { get; set; } = new List<ShipDto>();

        public MainForm()
        {
            InitializeComponents();
            net.OnMessageReceived += Net_OnMessageReceived;
            ownBoard.ShipDropped += OwnBoard_ShipDropped;
            ownBoard.CellClicked += OwnBoard_CellClickedForRemoval;
            btnGameOver.Click += BtnGameOver_Click;
        } 
        
        private void InitializeComponents()
        {
            // DPI-aware forma
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96f, 96f);
            this.ClientSize = new Size(1100, 720);
            this.Text = "Battleship Client";

            this.ClientSize = new Size(1050, 600);
            this.BackColor = ColorTranslator.FromHtml("#f8f9fa");

            // --- Valdikliai (laukeliai ir mygtukai) ---
            var l1 = new Label { Text = "Server (ws):", AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
            txtServer = new TextBox { Text = "ws://localhost:5000/ws/", Width = 260, Margin = new Padding(0, 2, 12, 0) };

            var l2 = new Label { Text = "Name:", AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
            txtName = new TextBox { Text = "Player", Width = 140, Margin = new Padding(0, 2, 12, 0) };

            btnConnect = new Button { Text = "Connect", AutoSize = true, Margin = new Padding(0, 2, 8, 0) };
            btnConnect.Click += BtnConnect_Click;

            btnRandomize = new Button { Text = "Randomize ships", AutoSize = true, Margin = new Padding(0, 2, 8, 0) };
            btnRandomize.Click += BtnRandomize_Click;

            btnPlaceShips = new Button { Text = "Place ships", AutoSize = true, Margin = new Padding(0, 2, 8, 0) };
            btnPlaceShips.Click += BtnPlaceShips_Click;

            btnReady = new Button { Text = "Ready", AutoSize = true, Margin = new Padding(0, 2, 8, 0) };
            btnReady.Click += BtnReady_Click;


            // naujas mygtukas (jei laukas jau deklaruotas – pernaudojam)
            btnVsBot ??= new Button { Text = "Žaisti su botu", AutoSize = true, Margin = new Padding(0, 2, 8, 0) };
            btnVsBot.Click -= BtnVsBot_Click; // kad nedubliuotų, jei jau pririštas
            btnVsBot.Click += BtnVsBot_Click;


            radioMiniGame = new RadioButton { Text = "Mini Game", Location = new Point(840, 8), AutoSize = true };
            radioStandartGame = new RadioButton { Text = "Standard Game", Location = new Point(950, 8), Checked = true };

            btnDoubleBombPowerUp = new Button { Text = "Double Bomb", Location = new Point(560, 88), Width = 150, Height = 30, Enabled = false, Visible = false };
            btnDoubleBombPowerUp.Click += BtnDoubleBombPowerUp_Click;

            btnGameOver = new Button { Text = "Game Over", Location = new Point(400, 44), Width = 100, Height = 30, Visible = false };

            lblStatus = new Label { Text = "Not connected", Location = new Point(10, 40), AutoSize = true };
            lblPowerUpInfo = new Label { Location = new Point(10, 60), AutoSize = true, Visible = false };

            ownBoard = new GameBoard { Location = new Point(80, 130) };
            enemyBoard = new GameBoard { Location = new Point(550, 130) };

            enemyBoard.CellClicked += EnemyBoard_CellClicked;

            // Dviejų stulpelių konteineris lentoms
            var boards = new TableLayoutPanel
            {

                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16),  

            }; 
            boards.RowStyles.Clear();
            boards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            boards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            boards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Lentos turi plėstis su langu

            boards.Controls.Add(ownBoard,  0, 0);
            boards.Controls.Add(enemyBoard, 1, 0);
            this.Controls.Add(boards); 


            // --- Lentos (po ownBoard/enemyBoard = new GameBoard() ir event'ų prisirišimo) ---

            // 1) Min. dydis: LabelMargin + CellPx*10 + 1 (tinklelio linijai)
            int cell = ownBoard.CellPx;           // 36 pagal tavo GameBoard
            int label = ownBoard.LabelMargin;     // 25 pagal tavo GameBoard
            int boardW = label + cell * 10 + 1;
            int boardH = label + cell * 10 + 1;

            ownBoard.MinimumSize   = new Size(boardW, boardH);
            enemyBoard.MinimumSize = new Size(boardW, boardH);

            // 2) Užpildyti savo lentelės cell'ę
            ownBoard.Dock   = DockStyle.Fill;
            enemyBoard.Dock = DockStyle.Fill;

            // 3) Vienodas tarpas nuo rėmų (neprivaloma, bet padeda vizualiai)
            ownBoard.Margin   = new Padding(24, 24, 24, 24);
            enemyBoard.Margin = new Padding(24, 24, 24, 24);


            // --- Laivų „preview“ panelė apačioje ---
            shipPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Padding = new Padding(12, 8, 12, 8)
            };
            this.Controls.Add(shipPanel);


            // Kiti mygtukai
            btnGameOver = new Button { Text = "Game Over", Visible = false };
            btnGameOver.Click += BtnGameOver_Click;

            this.Controls.AddRange(new Control[] {
                l1, txtServer, l2, txtName,
                btnConnect, btnRandomize, btnPlaceShips, radioMiniGame, radioStandartGame, btnReady, btnDoubleBombPowerUp, btnGameOver,
                lblStatus, lblPowerUpInfo, ownBoard, enemyBoard
            });

            btnReady.Enabled = false;
        }   




        private async void BtnVsBot_Click(object sender, EventArgs e)
        {
            try
            {
                // Jei dar neprisijungęs – prisijunk
                if (btnConnect.Enabled)
                {
                    await net.ConnectAsync(txtServer.Text);
                    lblStatus.Text = "Connected.";
                    btnConnect.Enabled = false;
                }

                // Vietoj "register" čia prašom žaisti su botu
                var playWithBot = new { type = "playWithBot", payload = new { playerName = txtName.Text } };
                await net.SendAsync(playWithBot);

                lblStatus.Text = "Kuriama partija su botu...";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nepavyko pradėti žaidimo su botu: " + ex.Message);
            }
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                await net.ConnectAsync(txtServer.Text);
                lblStatus.Text = "Connected.";
                var register = new { type = "register", payload = new { playerName = txtName.Text } };
                await net.SendAsync(register);
                btnConnect.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connect failed: " + ex.Message);
            }
        }

        private void BtnPlaceShips_Click(object sender, EventArgs e)
        {
            AbstractGameFactory factory = this.ReloadBoard();

            this.GameService.ResetMyFormOnly(this, myShips.Count == factory.GetShipsLength().Count, true, true);

            this.ShipPlacementService.HandlePlaceShip(placingHorizontal, shipPanel, factory.GetShipsLength());

            lblStatus.Text = "Drag ships onto your board. Use 'R' to rotate before dragging.";
        }

        private void OwnBoard_ShipDropped(ShipData ship, Point cell)
        {
            if (!placingShips) return;
            int x = cell.X;
            int y = cell.Y;

            if (!ShipPlacementService.CanPlaceShip(ownBoard, x, y, ship.Length, ship.Horizontal))
            {
                MessageBox.Show("Invalid placement here.");
                return;
            }

            ShipPlacementService.PlaceShip(ownBoard, x, y, ship.Length, ship.Horizontal);

            myShips.Add(new ShipDto
            {
                x = x,
                y = y,
                len = ship.Length,
                dir = ship.Horizontal ? "H" : "V"
            });

            AbstractGameFactory factory = radioMiniGame.Checked ? new MiniGameFactory() : new StandartGameFactory();
            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == factory.GetShipsLength().Count;

            var ctrl = shipPanel.Controls.Cast<Control>().FirstOrDefault(c => c.Tag is Guid g && g == ship.Id);
            if (ctrl != null) shipPanel.Controls.Remove(ctrl);
            if (shipPanel.Controls.Count == 0) shipPanel.Visible = false;

            lblStatus.Text = $"Placed {ship.Length}-cell ship at {x},{y} ({(ship.Horizontal ? "H" : "V")}).";
        }

        private void OwnBoard_CellClickedForRemoval(object sender, Point p)
        {
            (bool successful_removal, int len) = this.ShipPlacementService.RemoveShip(this.myShips, this.ownBoard, this.shipPanel, p);
            if (successful_removal && len >= 0)
            {
                btnReady.Enabled = myShips.Count == 10;
                lblStatus.Text = $"Removed {len}-cell ship from board.";
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.R && placingShips)
            {
                placingHorizontal = !placingHorizontal;

                foreach (ShipPreviewControl sp in shipPanel.Controls.OfType<ShipPreviewControl>())
                {
                    sp.Horizontal = placingHorizontal;
                    sp.Width = placingHorizontal ? sp.Length * 30 : 30;
                    sp.Height = placingHorizontal ? 30 : sp.Length * 30;
                    sp.Invalidate();
                }

                lblStatus.Text = $"Orientation changed to {(placingHorizontal ? "H" : "V")}";
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void EnemyBoard_CellClicked(object sender, Point p)
        {
            if (!this.isMyTurn) { this.lblStatus.Text = "Not your turn."; return; }
            this.lblStatus.Text = $"Firing at {p.X},{p.Y}...";
            var shot = new { type = "shot", payload = new { x = p.X, y = p.Y, doubleBomb = this.doubleBombActive } };
            if (this.doubleBombActive)
            {
                this.doubleBombActive = false;
                this.btnDoubleBombPowerUp.BackColor = SystemColors.Control;
                this.doubleBombsUsed += 1;
                if (this.doubleBombsUsed >= this.maxDoubleBombsCount)
                {
                    this.btnDoubleBombPowerUp.Enabled = false;
                    this.btnDoubleBombPowerUp.Visible = false;
                }
                UpdatePowerUpLabel();
            }
            await net.SendAsync(shot);
        }

        private void BtnRandomize_Click(object sender, EventArgs e)
        {
            AbstractGameFactory factory = this.ReloadBoard();
            myShips.Clear();
            (myShips, CellState[,] temp) = ShipPlacementService.RandomizeShips(factory.GetBoardSize(), factory.GetShipsLength());
            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == factory.GetShipsLength().Count;

            for (int r = 0; r < ownBoard.Size; r++)
                for (int c = 0; c < ownBoard.Size; c++)
                    ownBoard.SetCell(c, r, temp[r, c]);

            lblStatus.Text = $"Randomized {myShips.Count} ships.";
        }

        private async void BtnReady_Click(object sender, EventArgs e)
        {
            AbstractGameFactory factory = radioMiniGame.Checked ? new MiniGameFactory() : new StandartGameFactory();
            if (myShips.Count != factory.GetShipsLength().Count)
            {
                MessageBox.Show($"You must place all {factory.GetShipsLength().Count} ships before pressing Ready.");
                return;
            }

            var payload = new
            {
                ships = myShips,
                isStandartGame = radioStandartGame.Checked
            };
            var msg = new { type = "ready", payload = payload };
            await net.SendAsync(msg);
            lblStatus.Text = "Ready sent.";
            btnReady.Enabled = false;
            btnPlaceShips.Enabled = false;
            btnRandomize.Enabled = false;
            radioMiniGame.Enabled = false;
            radioStandartGame.Enabled = false;
            lblPowerUpInfo.Visible = true;

            if (factory.GetPowerups().TryGetValue("DoubleBomb", out int doubleBombsCount))
            {
                this.maxDoubleBombsCount = doubleBombsCount;
                this.doubleBombsUsed = 0;
                this.btnDoubleBombPowerUp.Enabled = true;
                this.btnDoubleBombPowerUp.Visible = true;
            }
            UpdatePowerUpLabel();
        }

        private void Net_OnMessageReceived(MessageDto dto)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Net_OnMessageReceived(dto)));
                return;
            }


            switch (dto.Type)
            {
                case "info":
                    if (dto.Payload.TryGetProperty("message", out var me))
                        lblStatus.Text = me.GetString();
                    break;

                case "startGame":
                    if (dto.Payload.TryGetProperty("yourId", out var yi)) myId = yi.GetString();
                    if (dto.Payload.TryGetProperty("opponentId", out var oi)) oppId = oi.GetString();
                    if (dto.Payload.TryGetProperty("current", out var cur))
                        isMyTurn = cur.GetString() == myId;
                    lblStatus.Text = $"Game started. Opponent: {dto.Payload.GetProperty("opponent").GetString()}. Your turn: {isMyTurn}";
                    break;

                case "turn":
                    if (dto.Payload.TryGetProperty("current", out var cur2))
                    {
                        isMyTurn = cur2.GetString() == myId;
                        lblStatus.Text = isMyTurn ? "Your turn" : "Opponent's turn";
                    }
                    break;

                case "shotResult":
                {
                    int x = dto.Payload.GetProperty("x").GetInt32();
                    int y = dto.Payload.GetProperty("y").GetInt32();
                    string res = dto.Payload.GetProperty("result").GetString(); // miss | hit | whole_ship_down
                    string targetId = dto.Payload.GetProperty("targetId").GetString();

                    var board = targetId == myId ? ownBoard : enemyBoard;

                    if (res == "miss")         board.SetCell(x, y, CellState.Miss);
                    else if (res == "hit")     board.SetCell(x, y, CellState.Hit);
                    else if (res == "whole_ship_down")
                                            board.SetCell(x, y, CellState.Whole_ship_down);  // ← perrašom į tamsiai raudoną
                    board.Invalidate();
                    break;
                }

                case "gameOver":
                    {
                        var winner = dto.Payload.GetProperty("winnerId").GetString();
                        MessageBox.Show(this, winner == myId ? "Laimėjai! 🎉" : "Pralaimėjai.", "Game over");

                        enemyBoard.Enabled = false;      // nebeleidžiam šaudyti
                        btnPlaceShips.Enabled = false;
                        btnRandomize.Enabled = false;
                        btnReady.Enabled = false;
                        btnVsBot.Enabled = true;         // leisk pradėti naują
                        lblStatus.Text = "Game over.";

                        break;
                    }

                case "error":
                    if (dto.Payload.TryGetProperty("message", out var err))
                        MessageBox.Show(err.GetString(), "Error");
                    break;
            }

            this.MessageService.HandleMessage(dto, this);

        }

        private async void BtnGameOver_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you want to play again?", "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                this.GameService.ResetForm(this, false);
            }
            else
            {
                this.Close();
            }

            btnGameOver.Visible = false;
        }

        public void BtnDoubleBombPowerUp_Click(object sender, EventArgs e)
        {
            if (!this.isMyTurn)
            {
                return;
            }
            if (doubleBombsUsed < maxDoubleBombsCount)
            {
                doubleBombActive = !doubleBombActive;
                btnDoubleBombPowerUp.BackColor = doubleBombActive ? Color.LightGreen : SystemColors.Control;
                lblStatus.Text = doubleBombActive ? "Double Bomb activated!" : "Double Bomb deactivated.";
            }
            else
            {
                this.doubleBombActive = false;
                this.btnDoubleBombPowerUp.Enabled = false;
                this.btnDoubleBombPowerUp.Visible = false;
            }
        }

        private AbstractGameFactory ReloadBoard()
        {
            AbstractGameFactory factory = radioMiniGame.Checked ? new MiniGameFactory() : new StandartGameFactory();
            this.Controls.Remove(ownBoard);
            this.Controls.Remove(enemyBoard);

            this.ownBoard = new GameBoard(factory.GetBoardSize()) { Location = new Point(80, 130) };
            this.ownBoard.ShipDropped += OwnBoard_ShipDropped;
            this.ownBoard.CellClicked += OwnBoard_CellClickedForRemoval;
            this.enemyBoard = new GameBoard(factory.GetBoardSize()) { Location = new Point(550, 130) };
            this.enemyBoard.CellClicked += EnemyBoard_CellClicked;

            this.Controls.Add(ownBoard);
            this.Controls.Add(enemyBoard);
            return factory;
        }

        private void UpdatePowerUpLabel()
        {
            this.lblPowerUpInfo.Text = $"PowerUp info:\nDouble bombs: x {this.maxDoubleBombsCount - this.doubleBombsUsed}";
        }
    }
}
