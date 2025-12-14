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
using BattleshipClient.ConsoleInterpreter;
using BattleshipClient.TemplateMethod;
using BattleshipClient.Views.Renderers;

namespace BattleshipClient
{
    public class MainForm : Form
    {
        // Text boxes:
        private TextBox txtServer;
        public TextBox txtName { get; private set; }

        // Buttons:
        private Button btnConnect;
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
        private Button btnShieldSafe, btnShieldInvisible, btnAreaShield;
        private Button btnHeal;

        // Labels:
        public Label lblStatus;
        public Label lblScoreboardBottom;
        public Label lblPowerUpInfo;
        private Label lblBoardStyle;

        // Panels and controls:
        public FlowLayoutPanel shipPanel;
        private FlowLayoutPanel topBar;
        private ComboBox cmbBoardStyle;
        private ComboBox cmbPlacementMode;

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
        private bool healActive = false;
        private int plusUsed = 0, xUsed = 0, superUsed = 0, healUsed = 0;
        private const int MaxPlus = 1, MaxX = 1, MaxSuper = 1, MaxHeal = 1;
        public bool doubleBombActive = false;
        public int maxDoubleBombsCount = 0;
        public int doubleBombsUsed = 0;
        bool powerUpsShown = false; // rodyti +/X/Super, kai žaidimas prasidėjęs 

        private bool shieldSafeActive = false, shieldInvisibleActive = false, areaShieldActive = false;
        private int safeShieldsUsed = 0, invisibleShieldsUsed = 0;
        private const int MaxSafeShields = 2;
        private const int MaxInvisibleShields = 2;

        // factories and services
        private AbstractGameFactory abstractFactory;
        private ShipPlacementService ShipPlacementService = new ShipPlacementService();
        private GameService GameService = new GameService();
        private SoundService soundService = new SoundService(new SoundFactory());
        private MessageService MessageService;

        // Other:
        public INetworkClient net { get; private set; }
        public GameCommandManager CommandManager = new GameCommandManager();
        private PlacementMode placementMode = PlacementMode.Random;
        private ContextMenuStrip randomizeMenu;
        private bool userChosePlacementMode = false;

        public MainForm(INetworkClient networkClient)
        {
            net = networkClient;

            InitializeComponents();
            net.OnMessageReceived += Net_OnMessageReceived;
            ownBoard.ShipDropped += OwnBoard_ShipDropped;
            ownBoard.CellClicked += OwnBoard_CellClickedForRemoval;
            btnGameOver.Click += BtnGameOver_Click;

            StartConsoleInterpreter();
        }

        private void InitializeComponents()
        {
            this.Text = "Battleship Client";
            this.ClientSize = new Size(1200, 800);
            this.BackColor = ColorTranslator.FromHtml("#f8f9fa");

            Label l1 = new Label { Text = "Server (ws):", Location = new Point(10, 10), AutoSize = true };
            txtServer = new TextBox { Text = "ws://localhost:5000/ws/", Location = new Point(100, 10), Width = 300 };
            Label l2 = new Label { Text = "Name:", Location = new Point(420, 10), AutoSize = true };
            txtName = new TextBox { Text = "Player", Location = new Point(470, 10), Width = 120 };

            btnConnect = new Button { Text = "Connect", Location = new Point(600, 8), Width = 80, Height = 30 };
            btnConnect.Click += BtnConnect_Click;

            btnPlaceShips = new Button { Text = "Place ships", Location = new Point(560, 44), Width = 130, Height = 30 };
            btnPlaceShips.Click += BtnPlaceShips_Click;

            btnReady = new Button { Text = "Ready", Location = new Point(700, 8), Width = 130, Height = 30 };
            btnReady.Click += BtnReady_Click;

            var btnPlayBot = new Button { Text = "Play vs Bot", Location = new Point(560, 80), Width = 130, Height = 30 };
            btnPlayBot.Click += BtnPlayBot_Click;
            this.Controls.Add(btnPlayBot);

            radioMiniGame = new RadioButton { Text = "Mini Game", Location = new Point(840, 8), AutoSize = true };
            radioStandartGame = new RadioButton { Text = "Standard Game", Location = new Point(950, 8), Checked = true };

            btnDoubleBombPowerUp = new Button { Text = "Double Bomb", Location = new Point(560, 88), Width = 150, Height = 30, Enabled = false, Visible = false };
            btnDoubleBombPowerUp.Click += BtnDoubleBombPowerUp_Click;

            btnSaveShipPlacement = new Button { Text = "Save ship placement", Location = new Point(840, 44), AutoSize = true, Visible = false, Enabled = false };
            btnSaveShipPlacement.Click += BtnSaveGameShipPlacement_Click;

            btnUseGameCopy = new Button { Text = "Use saved placement", Location = new Point(860, 44), AutoSize = true, Visible = true };
            btnUseGameCopy.Click += BtnUseGameCopy_Click;

            btnGameOver = new Button { Text = "Game Over", Location = new Point(400, 44), Width = 100, Height = 30, Visible = false };

            btnPlus = new Button { Text = "+ Shot", Location = new Point(560, 100), Visible = false, Enabled = false, AutoSize = true };
            btnX = new Button { Text = "X Shot", Visible = false, Enabled = false, AutoSize = true };
            btnSuper = new Button { Text = "Super", Visible = false, Enabled = false, AutoSize = true };
            btnHeal = new Button { Text = "Heal", Visible = false, Enabled = false, AutoSize = true };

            btnPlus.Click += (s, e) => { if (plusUsed >= MaxPlus) return; plusActive = !plusActive; btnPlus.BackColor = plusActive ? Color.LightGreen : SystemColors.Control; };
            btnX.Click += (s, e) => { if (xUsed >= MaxX) return; xActive = !xActive; btnX.BackColor = xActive ? Color.LightGreen : SystemColors.Control; };
            btnSuper.Click += (s, e) => { if (superUsed >= MaxSuper) return; superActive = !superActive; btnSuper.BackColor = superActive ? Color.LightGreen : SystemColors.Control; };
            btnHeal.Click += (s, e) =>
            {
                if (healUsed >= MaxHeal) return;

                // Įjungiam / išjungiam heal modus
                healActive = !healActive;

                // Jei įjungiam heal – išjungiam kitus power-up'us (kad vienu metu nebūtų du režimai)
                if (healActive)
                {
                    plusActive = xActive = superActive = false;
                    btnPlus.BackColor = SystemColors.Control;
                    btnX.BackColor = SystemColors.Control;
                    btnSuper.BackColor = SystemColors.Control;
                }

                btnHeal.BackColor = healActive ? Color.LightGreen : SystemColors.Control;
            };

            this.Controls.AddRange(new Control[] { btnPlus, btnX, btnSuper, btnHeal });

            btnShieldSafe = new Button { Text = "Shield (Safe)", Visible = false, Enabled = false, AutoSize = true };
            btnShieldInvisible = new Button { Text = "Shield (Invisible)", Visible = false, Enabled = false, AutoSize = true };
            btnAreaShield = new Button { Text = "Area Shield 3x3", Visible = false, Enabled = false, AutoSize = true };

            btnShieldSafe.Click += (s, e) =>
            {
                if (safeShieldsUsed >= MaxSafeShields) return;

                shieldSafeActive = !shieldSafeActive;
                shieldInvisibleActive = false;

                btnShieldSafe.BackColor = shieldSafeActive ? Color.LightGreen : SystemColors.Control;
                btnShieldInvisible.BackColor = SystemColors.Control;

                SyncPowerUpsUI();
            };

            btnShieldInvisible.Click += (s, e) =>
            {
                if (invisibleShieldsUsed >= MaxInvisibleShields) return;

                shieldInvisibleActive = !shieldInvisibleActive;
                shieldSafeActive = false;

                btnShieldInvisible.BackColor = shieldInvisibleActive ? Color.LightGreen : SystemColors.Control;
                btnShieldSafe.BackColor = SystemColors.Control;

                SyncPowerUpsUI();
            };

            btnAreaShield.Click += (s, e) =>
            {
                if (!(shieldSafeActive || shieldInvisibleActive))
                {
                    MessageBox.Show("First choose Shield (Safe) or Shield (Invisible).");
                    return;
                }

                areaShieldActive = !areaShieldActive;
                btnAreaShield.BackColor = areaShieldActive ? Color.LightGreen : SystemColors.Control;
            };

            lblStatus = new Label { Text = "Not connected", Location = new Point(10, 40), AutoSize = true };
            lblPowerUpInfo = new Label { Location = new Point(10, 60), AutoSize = true, Visible = false };

            ownBoard = new GameBoard { Location = new Point(80, 200) };
            enemyBoard = new GameBoard { Location = new Point(550, 200) };
            enemyBoard.CellClicked += EnemyBoard_CellClicked;

            this.gameTemplate = new GameTemplate(10);

            shipPanel = new FlowLayoutPanel
            {
                Location = new Point(100, 600),
                Size = new Size(450, 100),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            this.Controls.Add(shipPanel);
            this.Controls.AddRange(new Control[] {
                l1, txtServer, l2, txtName,
                btnConnect, btnPlaceShips, radioMiniGame, radioStandartGame, btnReady, btnSaveShipPlacement, btnUseGameCopy, btnDoubleBombPowerUp, btnGameOver,
                lblStatus, lblPowerUpInfo, ownBoard, enemyBoard
            });

            // --- Laivų išdėstymo strategijos pasirinkimas ---
            Label lblPlacementMode = new Label
            {
                Text = "Placement Mode:",
                Location = new Point(700, 44),
                AutoSize = true
            };

            cmbPlacementMode = new ComboBox
            {
                Location = new Point(700, 64),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbPlacementMode.Items.Add("Random");
            cmbPlacementMode.Items.Add("Edge");
            cmbPlacementMode.Items.Add("Spread Safe");

            cmbPlacementMode.SelectedIndex = 0;

            cmbPlacementMode.SelectedIndexChanged += (s, e) =>
            {
                userChosePlacementMode = true;
                var mode = cmbPlacementMode.SelectedIndex switch
                {
                    0 => PlacementMode.Random,
                    1 => PlacementMode.Edge,
                    2 => PlacementMode.SpreadSafe,
                    _ => PlacementMode.Random
                };

                SetPlacementMode(mode);
            };
            this.Controls.Add(lblPlacementMode);
            this.Controls.Add(cmbPlacementMode);

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

            cmbBoardStyle.Items.AddRange(Enum.GetNames(typeof(BoardStyle)));
            cmbBoardStyle.SelectedIndex = 0;
            cmbBoardStyle.SelectedIndexChanged += CmbBoardStyle_SelectedIndexChanged;

            this.Controls.Add(lblBoardStyle);
            this.Controls.Add(cmbBoardStyle);

            topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(555, 115, 90, 20),
                WrapContents = false
            };
            this.Controls.Add(topBar);

            topBar.Controls.Add(btnPlus);
            topBar.Controls.Add(btnX);
            topBar.Controls.Add(btnSuper);
            topBar.Controls.Add(btnHeal);
            topBar.Controls.Add(btnDoubleBombPowerUp);
            topBar.Controls.Add(btnShieldSafe);
            topBar.Controls.Add(btnShieldInvisible);
            topBar.Controls.Add(btnAreaShield);

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

            btnReplay = new Button { Text = "Replay", Location = new Point(480, 620), Width = 80, Height = 30, Visible = false };
            btnToStart = new Button { Text = "⏮ Start", Location = new Point(600, 620), Width = 80, Height = 30, Visible = false };
            btnPrev = new Button { Text = "◀ Prev", Location = new Point(700, 620), Width = 80, Height = 30, Visible = false };
            btnNext = new Button { Text = "Next ▶", Location = new Point(800, 620), Width = 80, Height = 30, Visible = false };
            btnToEnd = new Button { Text = "End ⏭", Location = new Point(900, 620), Width = 80, Height = 30, Visible = false };

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
            if (!placingShips) return;

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

                lblStatus.Text = $"Orientation changed to {(placingHorizontal ? "H" : "V")}.";
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
                    doubleBomb = this.doubleBombActive,
                    plusShape = this.plusActive,
                    xShape = this.xActive,
                    superDamage = this.superActive
                }
            };
            if (plusActive)
            {
                plusActive = false;
                plusUsed = MaxPlus;
                btnPlus.Enabled = false;
                btnPlus.Text = "+ Shot (used)";
                btnPlus.BackColor = SystemColors.Control;
            }
            if (xActive)
            {
                xActive = false;
                xUsed = MaxX;
                btnX.Enabled = false;
                btnX.Text = "X Shot (used)";
                btnX.BackColor = SystemColors.Control;
            }
            if (superActive)
            {
                superActive = false;
                superUsed = MaxSuper;
                btnSuper.Enabled = false;
                btnSuper.Text = "Super (used)";
                btnSuper.BackColor = SystemColors.Control;
            }

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
            (myShips, CellState[,] temp) = ShipPlacementService.RandomizeShips(this.gameTemplate.GameBoard.Size, this.gameTemplate.Ships, placementMode);
            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == this.gameTemplate.Ships.Count;

            for (int r = 0; r < ownBoard.Size; r++)
                for (int c = 0; c < ownBoard.Size; c++)
                    ownBoard.SetCell(c, r, temp[r, c]);
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
            cmbPlacementMode.Enabled = false;
            btnPlaceShips.Enabled = false;
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
            plusUsed = xUsed = superUsed = 0;
            plusActive = xActive = superActive = false;
            btnPlus.Text = "+ Shot"; btnPlus.BackColor = SystemColors.Control;
            btnX.Text = "X Shot"; btnX.BackColor = SystemColors.Control;
            btnSuper.Text = "Super"; btnSuper.BackColor = SystemColors.Control;

            powerUpsShown = true;
            SyncPowerUpsUI();
            placingShips = false;
            this.ownBoard.CellClicked -= OwnBoard_CellClickedForRemoval;
            this.ownBoard.CellClicked += OwnBoard_CellClickedForShield;

            safeShieldsUsed = invisibleShieldsUsed = 0;
            shieldSafeActive = shieldInvisibleActive = false;
            btnShieldSafe.Text = "Shield (Safe)";
            btnShieldInvisible.Text = "Shield (Invisible)";
            btnShieldSafe.BackColor = SystemColors.Control;
            btnShieldInvisible.BackColor = SystemColors.Control;
            areaShieldActive = false;
            btnAreaShield.BackColor = SystemColors.Control;
        }

        private async void BtnPlayBot_Click(object sender, EventArgs e)
        {
            if (userChosePlacementMode == false)
            {
                placementMode = PlacementMode.Random;
                SetPlacementMode(placementMode);
            }
            if (myShips.Count != this.gameTemplate.Ships.Count)
            {
                myShips.Clear();
                (myShips, CellState[,] temp) = ShipPlacementService.RandomizeShips(this.gameTemplate.GameBoard.Size, this.gameTemplate.Ships, placementMode);
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
            cmbPlacementMode.Enabled = false;
            btnPlaceShips.Enabled = false;
            radioMiniGame.Enabled = false;
            radioStandartGame.Enabled = false;
            lblPowerUpInfo.Visible = true;
            this.btnPlus.Visible = true;
            this.btnX.Visible = true;
            this.btnSuper.Visible = true;

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
            btnX.Text = "X Shot"; btnX.BackColor = SystemColors.Control;
            btnSuper.Text = "Super"; btnSuper.BackColor = SystemColors.Control;

            powerUpsShown = true;
            SyncPowerUpsUI();
            placingShips = false;
            this.ownBoard.CellClicked -= OwnBoard_CellClickedForRemoval;
            this.ownBoard.CellClicked += OwnBoard_CellClickedForShield;
        }

        private void Net_OnMessageReceived(MessageDto dto)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Net_OnMessageReceived(dto)));
                return;
            }

            // naujas tipas iš serverio – pilnas žaidimo atstatymas
            if (dto.Type == "restoreState")
            {
                ApplyRestoredState(dto.Payload);
                return;
            }

            if (MessageService == null) return;

            this.MessageService.HandleMessage(dto, this);
        }

        void SyncPowerUpsUI()
        {
            btnPlus.Visible = powerUpsShown;
            btnX.Visible = powerUpsShown;
            btnSuper.Visible = powerUpsShown;
            btnHeal.Visible = powerUpsShown;
            btnShieldSafe.Visible = powerUpsShown;
            btnShieldInvisible.Visible = powerUpsShown;
            btnAreaShield.Visible = powerUpsShown;

            btnPlus.Enabled = powerUpsShown && (plusUsed < MaxPlus);
            btnX.Enabled = powerUpsShown && (xUsed < MaxX);
            btnSuper.Enabled = powerUpsShown && (superUsed < MaxSuper);
            btnHeal.Enabled = powerUpsShown && (healUsed < MaxHeal);

            btnShieldSafe.Enabled = powerUpsShown && (safeShieldsUsed < MaxSafeShields);
            btnShieldInvisible.Enabled = powerUpsShown && (invisibleShieldsUsed < MaxInvisibleShields);
            btnAreaShield.Enabled = powerUpsShown
                && (safeShieldsUsed < MaxSafeShields || invisibleShieldsUsed < MaxInvisibleShields);
        }

        private async void BtnGameOver_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you want to play again?", "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                this.GameService.ResetForm(this, false);
                soundService.PlayMusic(MusicType.Background);
                this.btnPlaceShips.Enabled = true;
                this.btnReady.Enabled = true;
                this.cmbPlacementMode.Enabled = true;
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

        async void OwnBoard_CellClickedForShield(object sender, Point p)
        {
            if (!powerUpsShown) return;

            // 1) Heal režimas – prioritetinis
            if (healActive)
            {
                // Surandam laivą, kuriam priklauso paspaustas langelis
                var shipToHeal = myShips.FirstOrDefault(s =>
                {
                    bool horizontal = s.dir == "H";
                    if (horizontal)
                    {
                        return p.Y == s.y && p.X >= s.x && p.X < s.x + s.len;
                    }
                    else
                    {
                        return p.X == s.x && p.Y >= s.y && p.Y < s.y + s.len;
                    }
                });

                if (shipToHeal == null)
                {
                    lblStatus.Text = "Čia nėra tavo laivo – negaliu gydyti.";
                    return;
                }

                // Patikrinam visus to laivo langelius
                bool hasHit = false;
                bool hasIntact = false;
                var shipCells = new List<Point>();

                for (int i = 0; i < shipToHeal.len; i++)
                {
                    int cx = shipToHeal.dir == "H" ? shipToHeal.x + i : shipToHeal.x;
                    int cy = shipToHeal.dir == "H" ? shipToHeal.y : shipToHeal.y + i;

                    shipCells.Add(new Point(cx, cy));

                    var state = ownBoard.GetCell(cx, cy);
                    if (state == CellState.Hit) hasHit = true;
                    if (state == CellState.Ship) hasIntact = true;
                }

                if (!hasHit)
                {
                    lblStatus.Text = "Šis laivas dar nepažeistas – nėra ką gydyti.";
                    return;
                }

                if (!hasIntact)
                {
                    lblStatus.Text = "Laivas jau nuskandintas – jo gydyti nebegalima.";
                    return;
                }

                var healMsg = new
                {
                    type = "healShip",
                    payload = new
                    {
                        cells = shipCells
                                .Select(c => new { x = c.X, y = c.Y })
                                .ToArray()
                    }
                };
                await net.SendAsync(healMsg);

                foreach (var cell in shipCells)
                {
                    if (ownBoard.GetCell(cell.X, cell.Y) == CellState.Hit)
                        ownBoard.SetCell(cell.X, cell.Y, CellState.Ship);
                }
                ownBoard.Invalidate();

                healActive = false;
                healUsed++;
                btnHeal.BackColor = SystemColors.Control;
                if (healUsed >= MaxHeal)
                {
                    btnHeal.Enabled = false;
                    btnHeal.Text = "Heal used";
                }

                SyncPowerUpsUI();
                UpdatePowerUpLabel();
                return; // nebededam shield, jei gydėm visą laivą
            }

            // 2) Jei heal neaktyvus – elgiamės kaip seniau su shield
            if (!(shieldSafeActive || shieldInvisibleActive)) return;

            string mode = shieldSafeActive ? "safetiness" : "visibility";
            bool isArea = areaShieldActive;

            var msg = new
            {
                type = "placeShield",
                payload = new
                {
                    x = p.X,
                    y = p.Y,
                    placeShield = mode,
                    isArea = isArea
                }
            };

            await net.SendAsync(msg);

            areaShieldActive = false;
            btnAreaShield.BackColor = SystemColors.Control;

            if (shieldSafeActive)
            {
                shieldSafeActive = false;
                safeShieldsUsed++;
                btnShieldSafe.BackColor = SystemColors.Control;
                if (safeShieldsUsed >= MaxSafeShields)
                {
                    btnShieldSafe.Enabled = false;
                    btnShieldSafe.Text = "Shield (Safe) used";
                }
            }

            if (shieldInvisibleActive)
            {
                shieldInvisibleActive = false;
                invisibleShieldsUsed++;
                btnShieldInvisible.BackColor = SystemColors.Control;
                if (invisibleShieldsUsed >= MaxInvisibleShields)
                {
                    btnShieldInvisible.Enabled = false;
                    btnShieldInvisible.Text = "Shield (Invisible) used";
                }
            }

            SyncPowerUpsUI();
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
            this.ownBoard.Location = new Point(80, 200);
            this.ownBoard.ShipDropped += OwnBoard_ShipDropped;
            this.ownBoard.CellClicked += OwnBoard_CellClickedForRemoval;

            this.enemyBoard = this.gameTemplate.EnemyBoard;
            this.enemyBoard.Location = new Point(550, 200);
            this.enemyBoard.CellClicked += EnemyBoard_CellClicked;

            this.Controls.Add(ownBoard);
            this.Controls.Add(enemyBoard);
        }

        void UpdatePowerUpLabel()
        {
            lblPowerUpInfo.Text =
                "PowerUp info:\n" +
                $"- Double bombs: {maxDoubleBombsCount - doubleBombsUsed}\n" +
                $"- Heal: {MaxHeal - healUsed}";
        }

        public int GetShipCount()
        {
            return this.gameTemplate.Ships.Count;
        }

        private CellState MapIntToCellState(int v)
        {
            return v switch
            {
                1 => CellState.Ship,
                2 => CellState.Miss,
                3 => CellState.Hit,
                4 => CellState.Whole_ship_down,
                _ => CellState.Empty
            };
        }

        private void ApplyRestoredState(JsonElement payload)
        {
            try
            {
                myId = payload.GetProperty("yourId").GetString() ?? "";
                oppId = payload.GetProperty("opponentId").GetString() ?? "";
                myName = payload.GetProperty("yourName").GetString() ?? myName;
                oppName = payload.GetProperty("opponentName").GetString() ?? oppName;

                // 🔹 NUSKAITOM AR ŽAIDIMAS STANDARD / MINI
                bool isStandard = false;
                if (payload.TryGetProperty("isStandard", out var isStdElem) &&
                    isStdElem.ValueKind == JsonValueKind.True || isStdElem.ValueKind == JsonValueKind.False)
                {
                    // GetBoolean tiks, nes ValueKind bus True/False
                    isStandard = isStdElem.GetBoolean();
                }

                // 🔹 PERKURIAM GAME TEMPLATE PAGAL TIPĄ, KAD GAUTUME TEISINGUS POWERUPS (YPAČ DoubleBomb)
                this.abstractFactory = isStandard ? new StandartGameFactory() : new MiniGameFactory();
                this.gameTemplate = this.abstractFactory.CreateGame();

                var currentId = payload.GetProperty("current").GetString() ?? "";
                isMyTurn = (currentId == myId);

                lblStatus.Text = isMyTurn
                    ? "Your turn (restored)."
                    : "Opponent's turn (restored).";

                placingShips = false;
                shipPanel.Visible = false;
                btnReady.Enabled = false;
                btnPlaceShips.Enabled = false;
                cmbPlacementMode.Enabled = false;
                radioMiniGame.Enabled = false;
                radioStandartGame.Enabled = false;

                this.ownBoard.CellClicked -= OwnBoard_CellClickedForRemoval;
                this.ownBoard.CellClicked -= OwnBoard_CellClickedForShield;
                this.ownBoard.CellClicked += OwnBoard_CellClickedForShield;

                // --- Laivai ---
                myShips.Clear();
                if (payload.TryGetProperty("ships", out var shipsElem) &&
                    shipsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in shipsElem.EnumerateArray())
                    {
                        myShips.Add(new ShipDto
                        {
                            x = s.GetProperty("x").GetInt32(),
                            y = s.GetProperty("y").GetInt32(),
                            len = s.GetProperty("len").GetInt32(),
                            dir = s.GetProperty("dir").GetString() ?? "H"
                        });
                    }
                    ownBoard.Ships = myShips;
                }

                // --- Lentos ---
                var boardSelf = payload.GetProperty("boardSelf");
                var boardEnemy = payload.GetProperty("boardEnemy");

                var rowsSelf = boardSelf.EnumerateArray().ToList();
                var rowsEnemy = boardEnemy.EnumerateArray().ToList();

                for (int y = 0; y < rowsSelf.Count && y < ownBoard.Size; y++)
                {
                    var cellsSelf = rowsSelf[y].EnumerateArray().ToList();
                    var cellsEnemy = rowsEnemy[y].EnumerateArray().ToList();

                    for (int x = 0; x < cellsSelf.Count && x < ownBoard.Size; x++)
                    {
                        int vSelf = cellsSelf[x].GetInt32();
                        int vEnemy = cellsEnemy[x].GetInt32();

                        ownBoard.SetCell(x, y, MapIntToCellState(vSelf));
                        enemyBoard.SetCell(x, y, MapIntToCellState(vEnemy));
                    }
                }

                ownBoard.Invalidate();
                enemyBoard.Invalidate();

                // --- PowerUp būsenos iš serverio ---
                powerUpsShown = true;

                plusUsed = xUsed = superUsed = 0;
                healUsed = 0;
                safeShieldsUsed = invisibleShieldsUsed = 0;
                doubleBombsUsed = 0;

                if (payload.TryGetProperty("powerUps", out var pu) &&
                    pu.ValueKind == JsonValueKind.Object)
                {
                    if (pu.TryGetProperty("healUsed", out var hu)) healUsed = hu.GetInt32();
                    if (pu.TryGetProperty("safeShieldsUsed", out var ssu)) safeShieldsUsed = ssu.GetInt32();
                    if (pu.TryGetProperty("invisibleShieldsUsed", out var iu)) invisibleShieldsUsed = iu.GetInt32();
                    if (pu.TryGetProperty("plusUsed", out var plu)) plusUsed = plu.GetInt32();
                    if (pu.TryGetProperty("xUsed", out var xu)) xUsed = xu.GetInt32();
                    if (pu.TryGetProperty("superUsed", out var su)) superUsed = su.GetInt32();
                    if (pu.TryGetProperty("doubleBombUsed", out var dbu)) doubleBombsUsed = dbu.GetInt32();
                }

                // 🔹 DABAR, KAI ŽINOM GAME TYPE, ATSTATOM MAX DoubleBomb
                if (this.gameTemplate != null &&
                    this.gameTemplate.Powerups.TryGetValue("DoubleBomb", out var dbCount))
                {
                    this.maxDoubleBombsCount = dbCount;
                }
                else
                {
                    this.maxDoubleBombsCount = 0;
                }

                // Jei iš serverio ateina išprotėję skaičiai – apribojam
                if (maxDoubleBombsCount < 0) maxDoubleBombsCount = 0;
                if (doubleBombsUsed < 0) doubleBombsUsed = 0;
                if (doubleBombsUsed > maxDoubleBombsCount) doubleBombsUsed = maxDoubleBombsCount;

                plusActive = xActive = superActive = healActive = false;
                doubleBombActive = false;

                btnPlus.Text = plusUsed >= MaxPlus ? "+ Shot (used)" : "+ Shot";
                btnX.Text = xUsed >= MaxX ? "X Shot (used)" : "X Shot";
                btnSuper.Text = superUsed >= MaxSuper ? "Super (used)" : "Super";
                btnHeal.Text = healUsed >= MaxHeal ? "Heal used" : "Heal";
                btnShieldSafe.Text = safeShieldsUsed >= MaxSafeShields ? "Shield (Safe) used" : "Shield (Safe)";
                btnShieldInvisible.Text = invisibleShieldsUsed >= MaxInvisibleShields ? "Shield (Invisible) used" : "Shield (Invisible)";

                btnPlus.BackColor = SystemColors.Control;
                btnX.BackColor = SystemColors.Control;
                btnSuper.BackColor = SystemColors.Control;
                btnHeal.BackColor = SystemColors.Control;
                btnShieldSafe.BackColor = SystemColors.Control;
                btnShieldInvisible.BackColor = SystemColors.Control;
                btnAreaShield.BackColor = SystemColors.Control;
                btnDoubleBombPowerUp.BackColor = SystemColors.Control;

                btnPlus.Enabled = powerUpsShown && (plusUsed < MaxPlus);
                btnX.Enabled = powerUpsShown && (xUsed < MaxX);
                btnSuper.Enabled = powerUpsShown && (superUsed < MaxSuper);
                btnHeal.Enabled = powerUpsShown && (healUsed < MaxHeal);
                btnShieldSafe.Enabled = powerUpsShown && (safeShieldsUsed < MaxSafeShields);
                btnShieldInvisible.Enabled = powerUpsShown && (invisibleShieldsUsed < MaxInvisibleShields);
                btnAreaShield.Enabled = powerUpsShown &&
                    (safeShieldsUsed < MaxSafeShields || invisibleShieldsUsed < MaxInvisibleShields);

                // 🔹 Double Bomb mygtukas pagal MAX ir USED
                if (maxDoubleBombsCount > 0)
                {
                    btnDoubleBombPowerUp.Visible = powerUpsShown;
                    btnDoubleBombPowerUp.Enabled = doubleBombsUsed < maxDoubleBombsCount;
                    btnDoubleBombPowerUp.Text = doubleBombsUsed >= maxDoubleBombsCount
                        ? "Double Bomb used"
                        : "Double Bomb";
                }
                else
                {
                    btnDoubleBombPowerUp.Visible = false;
                    btnDoubleBombPowerUp.Enabled = false;
                }

                lblPowerUpInfo.Visible = true;
                UpdatePowerUpLabel();
                SyncPowerUpsUI();

                lblStatus.Text += " Game state restored.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Failed to restore game state: " + ex.Message;
            }
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

            if (Enum.TryParse<BoardStyle>(cmbBoardStyle.SelectedItem.ToString(), out var selectedStyle))
            {
                ownBoard.SetStyle(selectedStyle);
                enemyBoard.SetStyle(selectedStyle);
                lblStatus.Text = $"Lentos tema pakeista į: {selectedStyle}";
            }
        }

        public void FireShotFromConsole(int boardX, int boardY, bool usePlus, bool useXShape, bool useSuper)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => FireShotFromConsole(boardX, boardY, usePlus, useXShape, useSuper)));
                return;
            }

            if (enemyBoard == null)
            {
                Console.WriteLine("Priešo lenta dar nesukurta.");
                return;
            }

            if (boardX < 0 || boardY < 0 || boardX >= enemyBoard.Size || boardY >= enemyBoard.Size)
            {
                Console.WriteLine("Koordinatė už lentos ribų.");
                return;
            }

            if (usePlus && plusUsed >= MaxPlus)
            {
                Console.WriteLine("+ Shot jau panaudotas maksimaliai.");
                usePlus = false;
            }

            if (useXShape && xUsed >= MaxX)
            {
                Console.WriteLine("X Shot jau panaudotas maksimaliai.");
                useXShape = false;
            }

            if (useSuper && superUsed >= MaxSuper)
            {
                Console.WriteLine("Super Shot jau panaudotas maksimaliai.");
                useSuper = false;
            }

            plusActive = usePlus;
            xActive = useXShape;
            superActive = useSuper;

            var p = new Point(boardX, boardY);
            EnemyBoard_CellClicked(enemyBoard, p);
        }

        private void StartConsoleInterpreter()
        {
            Task.Run(() =>
            {
                var _ = new SystemConsoleIO();

                Console.WriteLine("=== Battleship konsolės komandos ===");
                Console.WriteLine("shoot B5   - paprastas šūvis");
                Console.WriteLine("plus C7    - + formos power-up šūvis");
                Console.WriteLine("x D4       - X formos power-up šūvis");
                Console.WriteLine("super A1   - super šūvis");
                Console.WriteLine("undo       - atšaukti paskutinį veiksmą (jei įmanoma)");
                Console.WriteLine("redo       - pakartoti atšauktą veiksmą");
                Console.WriteLine("help / ?   - pagalbos tekstas");
                Console.WriteLine("exit       - išeiti iš konsolės režimo");
                Console.WriteLine();

                var interpreter = new CommandInterpreter();

                while (true)
                {
                    Console.Write("> ");
                    var line = Console.ReadLine();
                    if (line is null)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Leidžiam interpreter'iui apdoroti "exit" ir mišrias komandas, pvz. "shoot B5; exit".
                    var shouldExit = interpreter.Interpret(line, this);
                    if (shouldExit)
                        break;
                }

                Console.WriteLine("Konsolės režimas baigtas.");
            });
        }

        private void SetPlacementMode(PlacementMode mode)
        {
            placementMode = mode;

            lblStatus.Text = mode switch
            {
                PlacementMode.Random =>
                    "Placement: Random — laivai dedami bet kurioje lentos vietoje.",
                PlacementMode.Edge =>
                    "Placement: Edge — laivai išdėstomi tik palei kraštus (viršų, dešinę, apačią, kairę).",
                PlacementMode.SpreadSafe =>
                    "Placement: Spread Safe — laivai nededami greta vienas kito, tarp jų visada tarpas.",
                _ => "Placement: Random"
            };

            BtnRandomize_Click(this, EventArgs.Empty);
        }
    }
}