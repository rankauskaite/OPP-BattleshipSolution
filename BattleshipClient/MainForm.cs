using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleshipClient.Models;
using BattleshipClient.Services;
using BattleshipClient.Observers;
using BattleshipClient.Factory;
using BattleshipClient.Commands;

namespace BattleshipClient
{
    public class MainForm : Form
    {
        // Text boxes:
        private TextBox txtServer;
        public TextBox txtName { get; private set; }

        // Buttons:
        private Button btnConnect;
        private Button btnRandomize;
        public Button btnReady;
        private Button btnPlaceShips;
        public Button btnGameOver;
        public RadioButton radioMiniGame;
        public RadioButton radioStandartGame;
        public Button btnDoubleBombPowerUp;
        public Button btnSaveShipPlacement;
        public Button btnUseGameCopy;
        private Button btnPrev;
        private Button btnNext;
        public Button btnReplay;
        private Button btnToStart;
        private Button btnToEnd;
        private Button btnPlus, btnX, btnSuper;

        // Labels:
        public Label lblStatus;
        public Label lblScoreboardBottom;
        public Label lblPowerUpInfo;
        private Label lblBoardStyle;

        // Panels and controls:
        public FlowLayoutPanel shipPanel;
        private FlowLayoutPanel topBar;
        private ComboBox cmbBoardStyle;

        // Objects related to game:
        public GameBoard ownBoard { get; set; }
        public GameBoard enemyBoard { get; set; }
        private GameTemplate gameTemplate;
        public List<ShipDto> myShips { get; private set; } = new List<ShipDto>();
        public List<Ship> ownShips { get; private set; } = new List<Ship>();
        public List<ShipDto> Ships { get; set; } = new List<ShipDto>();


        // Players info:
        public string myId { get; set; } = "";
        public string oppId { get; set; } = "";
        public string myName;
        public string oppName;


        // helper bools and counters:
        public bool isMyTurn = false;
        public bool placingShips = false;   // drag & drop state
        private bool placingHorizontal = true;  // drag & drop state
        private bool isReplayMode = false;
        private bool plusActive = false, xActive = false, superActive = false; 
        private int plusUsed = 0, xUsed = 0, superUsed = 0;
        private const int MaxPlus = 1, MaxX = 1, MaxSuper = 1;
        public bool doubleBombActive = false;
        public int maxDoubleBombsCount = 0;
        public int doubleBombsUsed = 0;
        bool powerUpsShown = false; // rodyti +/X/Super, kai žaidimas prasidėjęs

        // factories and services
        private AbstractGameFactory abstractFactory;
        private ShipPlacementService ShipPlacementService = new ShipPlacementService();
        private GameService GameService = new GameService();
        private SoundService soundService = new SoundService(new SoundFactory());
        private MessageService MessageService;

        // Other:
        public NetworkClient net { get; private set; } = new NetworkClient();
        public GameCommandManager CommandManager = new GameCommandManager(); 

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
            this.Text = "Battleship Client";
            this.ClientSize = new Size(1200, 700);
            this.BackColor = ColorTranslator.FromHtml("#f8f9fa");

            Label l1 = new Label { Text = "Server (ws):", Location = new Point(10, 10), AutoSize = true };
            txtServer = new TextBox { Text = "ws://localhost:5000/ws/", Location = new Point(100, 10), Width = 300 };
            Label l2 = new Label { Text = "Name:", Location = new Point(420, 10), AutoSize = true };
            txtName = new TextBox { Text = "Player", Location = new Point(470, 10), Width = 120 };

            btnConnect = new Button { Text = "Connect", Location = new Point(600, 8), Width = 80, Height = 30 };
            btnConnect.Click += BtnConnect_Click;

            btnRandomize = new Button { Text = "Randomize ships", Location = new Point(700, 8), Width = 130, Height = 30 };
            btnRandomize.Click += BtnRandomize_Click;

            btnPlaceShips = new Button { Text = "Place ships", Location = new Point(560, 44), Width = 130, Height = 30 };
            btnPlaceShips.Click += BtnPlaceShips_Click;

            btnReady = new Button { Text = "Ready", Location = new Point(700, 44), Width = 130, Height = 30 };
            btnReady.Click += BtnReady_Click;

            var btnPlayBot = new Button { Text = "Play vs Bot", Location = new Point(400, 88), Width = 130, Height = 30 };
            btnPlayBot.Click += BtnPlayBot_Click;

            radioMiniGame = new RadioButton { Text = "Mini Game", Location = new Point(840, 8), AutoSize = true };
            radioStandartGame = new RadioButton { Text = "Standard Game", Location = new Point(950, 8), Checked = true };

            btnDoubleBombPowerUp = new Button { Text = "Double Bomb", Location = new Point(560, 88), Width = 150, Height = 30, Enabled = false, Visible = false };
            btnDoubleBombPowerUp.Click += BtnDoubleBombPowerUp_Click;

            btnSaveShipPlacement = new Button { Text = "Save ship placement", Location = new Point(840, 44), AutoSize = true, Visible = false, Enabled = false };
            btnSaveShipPlacement.Click += BtnSaveGameShipPlacement_Click;

            btnUseGameCopy = new Button { Text = "Use saved placement", Location = new Point(840, 44), AutoSize = true, Visible = true };
            btnUseGameCopy.Click += BtnUseGameCopy_Click;

            btnGameOver = new Button { Text = "Game Over", Location = new Point(400, 44), Width = 100, Height = 30, Visible = false }; 


            btnPlus  = new Button { Text = "+ Shot",  Visible = false, Enabled = false, AutoSize = true };
            btnX     = new Button { Text = "X Shot",  Visible = false, Enabled = false, AutoSize = true };
            btnSuper = new Button { Text = "Super",   Visible = false, Enabled = false, AutoSize = true };

            btnPlus.Click  += (s,e)=> { if (plusUsed  >= MaxPlus) return; plusActive  = !plusActive;  btnPlus.BackColor  = plusActive  ? Color.LightGreen : SystemColors.Control; };
            btnX.Click     += (s,e)=> { if (xUsed     >= MaxX)    return; xActive     = !xActive;     btnX.BackColor     = xActive     ? Color.LightGreen : SystemColors.Control; };
            btnSuper.Click += (s,e)=> { if (superUsed >= MaxSuper)return; superActive = !superActive; btnSuper.BackColor = superActive ? Color.LightGreen : SystemColors.Control; };

            this.Controls.AddRange(new Control[]{ btnPlus, btnX, btnSuper });


            lblStatus = new Label { Text = "Not connected", Location = new Point(10, 40), AutoSize = true };
            lblPowerUpInfo = new Label { Location = new Point(10, 60), AutoSize = true, Visible = false };

            ownBoard = new GameBoard { Location = new Point(80, 150) };
            enemyBoard = new GameBoard { Location = new Point(550, 150) };
            enemyBoard.CellClicked += EnemyBoard_CellClicked;

            this.gameTemplate = new GameTemplate(10);

            shipPanel = new FlowLayoutPanel
            {
                Location = new Point(100, 530),
                Size = new Size(450, 100),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            this.Controls.Add(shipPanel);
            this.Controls.AddRange(new Control[] {
                l1, txtServer, l2, txtName,
                btnConnect, btnRandomize, btnPlaceShips, radioMiniGame, radioStandartGame, btnReady, btnSaveShipPlacement, btnUseGameCopy, btnDoubleBombPowerUp, btnGameOver,
                lblStatus, lblPowerUpInfo, ownBoard, enemyBoard
            });

            // --- Lentos temos pasirinkimas ---
            lblBoardStyle = new Label
            {
                Text = "Board Style:",
                Location = new Point(1050, 8),
                AutoSize = true
            };

            cmbBoardStyle = new ComboBox
            {
                Location = new Point(1050, 28),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Užpildom visus stilius
            cmbBoardStyle.Items.AddRange(Enum.GetNames(typeof(BoardStyle)));
            cmbBoardStyle.SelectedIndex = 0; // Classic pagal nutylėjimą
            cmbBoardStyle.SelectedIndexChanged += CmbBoardStyle_SelectedIndexChanged;

            // Pridedam į formą
            this.Controls.Add(lblBoardStyle);
            this.Controls.Add(cmbBoardStyle);

            topBar = new FlowLayoutPanel {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(600, 80, 90, 20),
                WrapContents = false
            };
            this.Controls.Add(topBar);

            topBar.Controls.Add(btnPlayBot); 
            topBar.Controls.Add(btnPlus);
            topBar.Controls.Add(btnX);
            topBar.Controls.Add(btnSuper);
            topBar.Controls.Add(btnDoubleBombPowerUp);

            lblScoreboardBottom = new Label
            {
                Name = "lblScoreboardBottom",
                Text = "Scoreboard:",
                Dock = DockStyle.Bottom,
                Height = 70,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold),
                BackColor = ColorTranslator.FromHtml("#e9ecef"),
                Padding = new Padding(0, 6, 0, 6)
            };
            this.Controls.Add(lblScoreboardBottom);
            this.Controls.SetChildIndex(lblScoreboardBottom, 0);

            btnReady.Enabled = false;
            soundService.PlayMusic(MusicType.Background);

            btnReplay = new Button { Text = "Replay", Location = new Point(480, 550), Width = 80, Height = 30, Visible = false };
            btnToStart = new Button { Text = "⏮ Start", Location = new Point(600, 550), Width = 80, Height = 30, Visible = false };
            btnPrev = new Button { Text = "◀ Prev", Location = new Point(700, 550), Width = 80, Height = 30, Visible = false };
            btnNext = new Button { Text = "Next ▶", Location = new Point(800, 550), Width = 80, Height = 30, Visible = false };
            btnToEnd = new Button { Text = "End ⏭", Location = new Point(900, 550), Width = 80, Height = 30, Visible = false };

            btnToStart.Click += BtnToStart_Click;
            btnToEnd.Click += BtnToEnd_Click;
            btnReplay.Click += BtnReplay_Click;
            btnPrev.Click += BtnPrev_Click;
            btnNext.Click += BtnNext_Click;

            this.Controls.AddRange(new Control[] { btnReplay, btnPrev, btnNext, btnToStart, btnToEnd });
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
                myName = txtName.Text;
                MessageService = new MessageService(myName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connect failed: " + ex.Message);
            }
        }

        private void BtnPlaceShips_Click(object sender, EventArgs e)
        {
            this.ReloadBoard();

            this.GameService.ResetMyFormOnly(this, myShips.Count == this.gameTemplate.Ships.Count, true, true);

            this.ShipPlacementService.HandlePlaceShip(placingHorizontal, shipPanel, this.gameTemplate.Ships);

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

            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == this.gameTemplate.Ships.Count;

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
                btnReady.Enabled = myShips.Count == this.gameTemplate.Ships.Count;
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
            var shot = new
            {
                type = "shot",
                payload = new
                {
                    x = p.X,
                    y = p.Y,
                    doubleBomb = this.doubleBombActive,  // likusi logika kaip buvo
                    plusShape = this.plusActive,
                    xShape = this.xActive,
                    superDamage = this.superActive
                }
            }; 
            if (plusActive)  { plusActive = false; plusUsed  = MaxPlus;  btnPlus.Enabled  = false; btnPlus.Text  = "+ Shot (used)"; btnPlus.BackColor  = SystemColors.Control; }
            if (xActive)     { xActive    = false; xUsed     = MaxX;     btnX.Enabled     = false; btnX.Text     = "X Shot (used)";  btnX.BackColor     = SystemColors.Control; }
            if (superActive) { superActive= false; superUsed = MaxSuper; btnSuper.Enabled = false; btnSuper.Text = "Super (used)";   btnSuper.BackColor = SystemColors.Control; }

           SyncPowerUpsUI();

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
            this.ReloadBoard();
            myShips.Clear();
            (myShips, CellState[,] temp) = ShipPlacementService.RandomizeShips(this.gameTemplate.GameBoard.Size, this.gameTemplate.Ships);
            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == this.gameTemplate.Ships.Count;

            for (int r = 0; r < ownBoard.Size; r++)
                for (int c = 0; c < ownBoard.Size; c++)
                    ownBoard.SetCell(c, r, temp[r, c]);

            lblStatus.Text = $"Randomized {myShips.Count} ships.";
        }

        private async void BtnReady_Click(object sender, EventArgs e)
        {
            if (myShips.Count != this.gameTemplate.Ships.Count)
            {
                MessageBox.Show($"You must place all {this.gameTemplate.Ships.Count} ships before pressing Ready.");
                return;
            }

            // Konvertuojame ShipDto į Ship
            ownShips.Clear();
            foreach (var shipDto in myShips)
            {
                ownShips.Add(new Ship(new GameEventManager(), shipDto, myName));
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
            btnUseGameCopy.Visible = false;
            btnSaveShipPlacement.Visible = true;
            btnSaveShipPlacement.Enabled = true;

            if (this.gameTemplate.Powerups.TryGetValue("DoubleBomb", out int doubleBombsCount))
            {
                this.maxDoubleBombsCount = doubleBombsCount;
                this.doubleBombsUsed = 0;
                this.btnDoubleBombPowerUp.Enabled = true;
                this.btnDoubleBombPowerUp.Visible = true;
            }
            UpdatePowerUpLabel();
            plusUsed = xUsed = superUsed = 0;      // reset skaitiklių
            plusActive = xActive = superActive = false;
            btnPlus.Text = "+ Shot"; btnPlus.BackColor = SystemColors.Control;
            btnX.Text = "X Shot";   btnX.BackColor   = SystemColors.Control;
            btnSuper.Text = "Super";btnSuper.BackColor= SystemColors.Control;

            powerUpsShown = true;                  // Žaidimas prasidėjo – rodom
            SyncPowerUpsUI();     
        }

        private async void BtnPlayBot_Click(object sender, EventArgs e)
        {
            if (myShips.Count != this.gameTemplate.Ships.Count)
            {
                myShips.Clear();
                (myShips, CellState[,] temp) = ShipPlacementService.RandomizeShips(this.gameTemplate.GameBoard.Size, this.gameTemplate.Ships);
                ownBoard.Ships = myShips;
                ownBoard.Invalidate();
                for (int r = 0; r < ownBoard.Size; r++)
                    for (int c = 0; c < ownBoard.Size; c++)
                        ownBoard.SetCell(c, r, temp[r, c]);
            }

            var payload = new { ships = myShips, isStandartGame = radioStandartGame.Checked };
            await net.SendAsync(new { type = "playBot", payload });

            lblStatus.Text = "Play vs Bot: laukiam starto...";
            btnReady.Enabled = false;
            btnPlaceShips.Enabled = false;
            btnRandomize.Enabled = false;
            radioMiniGame.Enabled = false;
            radioStandartGame.Enabled = false;
            lblPowerUpInfo.Visible = true;
            this.btnPlus.Visible = true;
            this.btnX.Visible = true;
            this.btnSuper.Visible = true;

            // (nebūtina, bet jei nori – kopija iš BtnReady_Click: įjungia Double Bomb pagal factory)
            if (this.gameTemplate.Powerups.TryGetValue("DoubleBomb", out int doubleBombsCount))
            {
                this.maxDoubleBombsCount = doubleBombsCount;
                this.doubleBombsUsed = 0;
                this.btnDoubleBombPowerUp.Enabled = true;
                this.btnDoubleBombPowerUp.Visible = true;
                UpdatePowerUpLabel();
            } 
            plusUsed = xUsed = superUsed = 0;
            plusActive = xActive = superActive = false;
            btnPlus.Text = "+ Shot"; btnPlus.BackColor = SystemColors.Control;
            btnX.Text = "X Shot";   btnX.BackColor   = SystemColors.Control;
            btnSuper.Text = "Super";btnSuper.BackColor= SystemColors.Control;

            powerUpsShown = true;
            SyncPowerUpsUI(); 
        }

        private void Net_OnMessageReceived(MessageDto dto)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Net_OnMessageReceived(dto)));
                return;
            }

            if (MessageService == null) return;

            this.MessageService.HandleMessage(dto, this);
        }

        private void SyncPowerUpsUI()
        {
            // Matomumas: tik ar žaidimas vyksta
            btnPlus.Visible  = powerUpsShown;
            btnX.Visible     = powerUpsShown;
            btnSuper.Visible = powerUpsShown;

            // Įjungimas: jei dar nepanaudota
            btnPlus.Enabled  = powerUpsShown && (plusUsed  < MaxPlus);
            btnX.Enabled     = powerUpsShown && (xUsed     < MaxX);
            btnSuper.Enabled = powerUpsShown && (superUsed < MaxSuper);
        }


        private async void BtnGameOver_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you want to play again?", "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                this.GameService.ResetForm(this, false);
                soundService.PlayMusic(MusicType.Background);
                this.btnPlaceShips.Enabled = true;
                this.btnRandomize.Enabled = true;
                this.btnReady.Enabled = true;
                this.btnUseGameCopy.Enabled = true;
                this.btnUseGameCopy.Visible = true;
                this.btnSaveShipPlacement.Visible = false;
                this.btnNext.Visible = false;
                this.btnPrev.Visible = false;
                this.btnReplay.Visible = false; 
                powerUpsShown = false;
                SyncPowerUpsUI();
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
                SyncPowerUpsUI();
            }
        }

        public async void BtnSaveGameShipPlacement_Click(object sender, EventArgs e)
        {
            var saveCopyMsg = new { type = "copyGame" };
            await net.SendAsync(saveCopyMsg);
            this.btnSaveShipPlacement.Visible = false;
        }

        public async void BtnUseGameCopy_Click(object sender, EventArgs e)
        {
            var useCopyMsg = new { type = "useGameCopy" };
            await net.SendAsync(useCopyMsg);
            this.btnUseGameCopy.Enabled = false;
        }

        private void ReloadBoard()
        {
            if (this.ownBoard != null)
            {
                this.ownBoard.ShipDropped -= OwnBoard_ShipDropped;
                this.ownBoard.CellClicked -= OwnBoard_CellClickedForRemoval;
            }
            if (this.enemyBoard != null)
            {
                this.enemyBoard.CellClicked -= EnemyBoard_CellClicked;
            }

            this.Controls.Remove(ownBoard);
            this.Controls.Remove(enemyBoard);

            this.abstractFactory = radioMiniGame.Checked ? new MiniGameFactory() : new StandartGameFactory();
            this.gameTemplate = this.abstractFactory.CreateGame();

            this.ownBoard = this.gameTemplate.GameBoard;
            this.ownBoard.Location = new Point(80, 130);
            this.ownBoard.ShipDropped += OwnBoard_ShipDropped;
            this.ownBoard.CellClicked += OwnBoard_CellClickedForRemoval;

            this.enemyBoard = this.gameTemplate.EnemyBoard;
            this.enemyBoard.Location = new Point(550, 130);
            this.enemyBoard.CellClicked += EnemyBoard_CellClicked;

            this.Controls.Add(ownBoard);
            this.Controls.Add(enemyBoard);
        }

        private void UpdatePowerUpLabel()
        {
            this.lblPowerUpInfo.Text = $"PowerUp info:\nDouble bombs: x {this.maxDoubleBombsCount - this.doubleBombsUsed}";
        }

        public int GetShipCount()
        {
            return this.gameTemplate.Ships.Count;
        }

        private void BtnReplay_Click(object sender, EventArgs e)
        {
            if (CommandManager.TotalCommands == 0)
            {
                MessageBox.Show("No replay data available!");
                return;
            }

            isReplayMode = true;
            lblStatus.Text = "Replay Mode Active";

            btnPrev.Visible = true;
            btnNext.Visible = true;
            btnToStart.Visible = true;
            btnToEnd.Visible = true;
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (isReplayMode && CommandManager.CanUndo)
                CommandManager.Undo();
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (isReplayMode && CommandManager.CanRedo)
                CommandManager.Redo();
        }

        private void BtnToStart_Click(object sender, EventArgs e)
        {
            if (isReplayMode)
            {
                CommandManager.UndoAll();
                lblStatus.Text = "Returned to game start.";
            }
        }

        private void BtnToEnd_Click(object sender, EventArgs e)
        {
            if (isReplayMode)
            {
                CommandManager.RedoAll();
                lblStatus.Text = "Jumped to latest move.";
            }
        }

        public void UpdateScoreboardUI(JsonElement payload)
        {
            try
            {
                var p1 = payload.GetProperty("p1").GetString();
                var p2 = payload.GetProperty("p2").GetString();
                var h1 = payload.GetProperty("hits1").GetInt32();
                var h2 = payload.GetProperty("hits2").GetInt32();
                var w1 = payload.GetProperty("wins1").GetInt32();
                var w2 = payload.GetProperty("wins2").GetInt32();

                lblScoreboardBottom.Text =
                    "Scoreboard\n" +
                    $"{p1}: Hits {h1}, Wins {w1}\n" +
                    $"{p2}: Hits {h2}, Wins {w2}";
            }
            catch
            {
                lblScoreboardBottom.Text = "Scoreboard: (n/a)";
            }
        }

        private void CmbBoardStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbBoardStyle.SelectedItem == null)
                return;

            // konvertuojam pasirinkimą į enum reikšmę
            if (Enum.TryParse<BoardStyle>(cmbBoardStyle.SelectedItem.ToString(), out var selectedStyle))
            {
                ownBoard.SetStyle(selectedStyle);
                enemyBoard.SetStyle(selectedStyle);
                lblStatus.Text = $"Lentos tema pakeista į: {selectedStyle}";
            }
        }
    }
}