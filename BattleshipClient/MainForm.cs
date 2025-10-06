using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleshipClient.Models;

namespace BattleshipClient
{
    public class MainForm : Form
    {
        private TextBox txtServer;
        private TextBox txtName;
        private Button btnConnect;
        private Button btnRandomize;
        private Button btnReady;
        private Button btnPlaceShips;
        private Button btnGameOver; // Naujas mygtukas 
        private Button btnVsBot;
        private Label lblStatus;
        private Label lblScoreboard;
        private GameBoard ownBoard;
        private GameBoard enemyBoard;
        private FlowLayoutPanel shipPanel;

        private NetworkClient net = new NetworkClient();

        // state
        private List<ShipDto> myShips = new();
        private bool isMyTurn = false;
        private string myId = "";
        private string oppId = "";

        // drag & drop state
        private bool placingShips = false;
        private bool placingHorizontal = true;

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

            // statuso eilutė po top bar'u
            lblStatus = new Label { Text = "Not connected", Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8, 6, 8, 6) };

            // Scoreboard
            lblScoreboard = new Label
            {
                Text = "Scoreboard:",
                AutoSize = true,
                Padding = new Padding(8, 6, 8, 6),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            this.Controls.Add(lblScoreboard);

            // --- Viršutinė juosta (FlowLayoutPanel) ---
            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(8, 8, 8, 0)
            };
            topBar.Controls.AddRange(new Control[]
            {
                l1, txtServer,
                l2, txtName,
                btnConnect, btnRandomize, btnPlaceShips, btnReady, btnVsBot
            });
            this.Controls.Add(topBar);
            this.Controls.Add(lblStatus); // įdedame po topBar (DockStyle.Top)

            // --- Lentos ---
            ownBoard = new GameBoard();
            enemyBoard = new GameBoard();
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

            boards.Controls.Add(ownBoard, 0, 0);
            boards.Controls.Add(enemyBoard, 1, 0);
            this.Controls.Add(boards);


            // --- Lentos (po ownBoard/enemyBoard = new GameBoard() ir event'ų prisirišimo) ---

            // 1) Min. dydis: LabelMargin + CellPx*10 + 1 (tinklelio linijai)
            int cell = ownBoard.CellPx;           // 36 pagal tavo GameBoard
            int label = ownBoard.LabelMargin;     // 25 pagal tavo GameBoard
            int boardW = label + cell * 10 + 1;
            int boardH = label + cell * 10 + 1;

            ownBoard.MinimumSize = new Size(boardW, boardH);
            enemyBoard.MinimumSize = new Size(boardW, boardH);

            // 2) Užpildyti savo lentelės cell'ę
            ownBoard.Dock = DockStyle.Fill;
            enemyBoard.Dock = DockStyle.Fill;

            // 3) Vienodas tarpas nuo rėmų (neprivaloma, bet padeda vizualiai)
            ownBoard.Margin = new Padding(24, 24, 24, 24);
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
            placingShips = true;
            myShips.Clear();
            ownBoard.ClearBoard();
            shipPanel.Controls.Clear();
            shipPanel.Visible = true;
            btnReady.Enabled = myShips.Count == 10;

            int[] shipLens = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
            foreach (var len in shipLens)
            {
                var preview = new ShipPreviewControl(len) { Horizontal = placingHorizontal };
                preview.MouseDown += (s, ev) =>
                {
                    if (ev.Button == MouseButtons.Left)
                    {
                        var p = (ShipPreviewControl)s;
                        var data = new ShipData { Id = p.Id, Length = p.Length, Horizontal = p.Horizontal };
                        p.DoDragDrop(data, DragDropEffects.Copy);
                    }
                };
                shipPanel.Controls.Add(preview);
            }

            lblStatus.Text = "Drag ships onto your board. Use 'R' to rotate before dragging.";
        }

        private void OwnBoard_ShipDropped(ShipData ship, Point cell)
        {
            if (!placingShips) return;
            int x = cell.X;
            int y = cell.Y;

            if (!CanPlaceShip(x, y, ship.Length, ship.Horizontal))
            {
                MessageBox.Show("Invalid placement here.");
                return;
            }

            PlaceShip(x, y, ship.Length, ship.Horizontal);

            myShips.Add(new ShipDto
            {
                x = x,
                y = y,
                len = ship.Length,
                dir = ship.Horizontal ? "H" : "V"
            });
            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == 10;

            var ctrl = shipPanel.Controls.Cast<Control>().FirstOrDefault(c => c.Tag is Guid g && g == ship.Id);
            if (ctrl != null) shipPanel.Controls.Remove(ctrl);
            if (shipPanel.Controls.Count == 0) shipPanel.Visible = false;

            lblStatus.Text = $"Placed {ship.Length}-cell ship at {x},{y} ({(ship.Horizontal ? "H" : "V")}).";
        }

        private void OwnBoard_CellClickedForRemoval(object sender, Point p)
        {
            foreach (var s in myShips.ToList())
            {
                int x = s.x, y = s.y, len = s.len;
                bool horiz = s.dir == "H";
                for (int i = 0; i < len; i++)
                {
                    int cx = x + (horiz ? i : 0);
                    int cy = y + (horiz ? 0 : i);
                    if (p.X == cx && p.Y == cy)
                    {
                        for (int j = 0; j < len; j++)
                        {
                            int px = x + (horiz ? j : 0);
                            int py = y + (horiz ? 0 : j);
                            ownBoard.SetCell(px, py, CellState.Empty);
                        }

                        var preview = new ShipPreviewControl(len) { Horizontal = horiz };
                        preview.MouseDown += (s, ev) =>
                        {
                            if (ev.Button == MouseButtons.Left)
                            {
                                var p2 = (ShipPreviewControl)s;
                                var data = new ShipData { Id = p2.Id, Length = p2.Length, Horizontal = p2.Horizontal };
                                p2.DoDragDrop(data, DragDropEffects.Copy);
                            }
                        };
                        shipPanel.Controls.Add(preview);
                        shipPanel.Visible = true;

                        myShips.Remove(s);
                        ownBoard.Ships = myShips;
                        ownBoard.Invalidate();
                        btnReady.Enabled = myShips.Count == 10;
                        lblStatus.Text = $"Removed {len}-cell ship from board.";
                        return;
                    }
                }
            }
        }

        private bool CanPlaceShip(int x, int y, int len, bool horiz)
        {
            if (horiz && x + len > GameBoard.Size) return false;
            if (!horiz && y + len > GameBoard.Size) return false;

            for (int i = 0; i < len; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                if (ownBoard.GetCell(cx, cy) != CellState.Empty) return false;
            }
            return true;
        }

        public void PlaceShip(int x, int y, int len, bool horiz)
        {
            for (int i = 0; i < len; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                ownBoard.SetCell(cx, cy, CellState.Ship);
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
            if (!isMyTurn) { lblStatus.Text = "Not your turn."; return; }
            lblStatus.Text = $"Firing at {p.X},{p.Y}...";
            var shot = new { type = "shot", payload = new { x = p.X, y = p.Y } };
            await net.SendAsync(shot);
        }

        private void BtnRandomize_Click(object sender, EventArgs e) => RandomizeShips();

        private async void BtnReady_Click(object sender, EventArgs e)
        {
            if (myShips.Count != 10)
            {
                MessageBox.Show("You must place all ships before pressing Ready.");
                return;
            }

            var payload = new { ships = myShips };
            var msg = new { type = "ready", payload = payload };
            await net.SendAsync(msg);
            lblStatus.Text = "Ready sent.";
            btnReady.Enabled = false;
            btnPlaceShips.Enabled = false;
            btnRandomize.Enabled = false;
        }

        private void RandomizeShips()
        {
            var lens = new int[] { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
            var rnd = new Random();
            myShips.Clear();
            var temp = new CellState[GameBoard.Size, GameBoard.Size];

            foreach (var len in lens)
            {
                bool placed = false;
                int tries = 0;
                while (!placed && tries < 200)
                {
                    tries++;
                    bool horiz = rnd.Next(2) == 0;
                    int x = rnd.Next(0, GameBoard.Size - (horiz ? len - 1 : 0));
                    int y = rnd.Next(0, GameBoard.Size - (horiz ? 0 : len - 1));
                    bool ok = true;
                    for (int i = 0; i < len; i++)
                    {
                        int cx = x + (horiz ? i : 0);
                        int cy = y + (horiz ? 0 : i);
                        if (temp[cy, cx] != CellState.Empty) { ok = false; break; }
                    }
                    if (ok)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            int cx = x + (horiz ? i : 0);
                            int cy = y + (horiz ? 0 : i);
                            temp[cy, cx] = CellState.Ship;
                        }
                        myShips.Add(new ShipDto { x = x, y = y, len = len, dir = horiz ? "H" : "V" });
                        ownBoard.Ships = myShips;
                        ownBoard.Invalidate();
                        btnReady.Enabled = myShips.Count == 10;
                        placed = true;
                    }
                }
            }

            for (int r = 0; r < GameBoard.Size; r++)
                for (int c = 0; c < GameBoard.Size; c++)
                    ownBoard.SetCell(c, r, temp[r, c]);

            lblStatus.Text = $"Randomized {myShips.Count} ships.";
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

                        if (res == "miss") board.SetCell(x, y, CellState.Miss);
                        else if (res == "hit") board.SetCell(x, y, CellState.Hit);
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

                case "scoreUpdate":
                    HandleScoreUpdate(dto.Payload);
                    break;
            }
        }

        private async void BtnGameOver_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you want to play again?", "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                ownBoard.ClearBoard();
                enemyBoard.ClearBoard();
                myShips.Clear();
                shipPanel.Controls.Clear();
                shipPanel.Visible = false;
                btnReady.Enabled = false;
                placingShips = false;
                isMyTurn = false;

                lblStatus.Text = "Waiting for new game...";
                var register = new { type = "register", payload = new { playerName = txtName.Text } };
                await net.SendAsync(register);
            }
            else
            {
                this.Close();
            }

            btnGameOver.Visible = false;
        }

        private void HandleScoreUpdate(JsonElement payload)
        {
            string p1 = payload.GetProperty("p1").GetString();
            string p2 = payload.GetProperty("p2").GetString();
            int hits1 = payload.GetProperty("hits1").GetInt32();
            int hits2 = payload.GetProperty("hits2").GetInt32();
            int wins1 = payload.GetProperty("wins1").GetInt32();
            int wins2 = payload.GetProperty("wins2").GetInt32();

            lblScoreboard.Text = $"Scoreboard: \n {p1}: {hits1} hits, {wins1} wins \n {p2}: {hits2} hits, {wins2} wins";
        }

        private void InitializeComponent() { }
    }
}
