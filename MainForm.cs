using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using nClam;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Collections.Concurrent;


namespace SalDefender
{
    public class MainForm : Form
    {

        private bool isDarkMode = false;

        // Definizione Palette
        private readonly Color DarkBg = Color.FromArgb(32, 32, 32);
        private readonly Color DarkPanel = Color.FromArgb(45, 45, 45);
        private readonly Color DarkText = Color.WhiteSmoke;
        private readonly Color LightBg = Color.White;
        private readonly Color LightText = Color.FromArgb(45, 45, 45);
        private Button scanButton;
        private Button updateButton;
        private Button settingsButton;
        private ListBox resultsList;
        private Label titleLabel;
        private PictureBox logoBox;
        private TextBox urlTextBox;
        private Button downloadScanButton;
        private Button diskScanButton;
        private ComboBox driveComboBox;
        private ProgressBar scanProgressBar;
        private Label progressLabel;
        private Button cancelScanButton;
        private CancellationTokenSource cancellationTokenSource;

        // Dichiarazione in alto con le altre variabili
        private Label liveStatusLabel;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Process clamdProcess; // Questa è la variabile che mancava

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_ALPHA = 0x2;

        private FileSystemWatcher liveWatcher;
        private bool isLiveProtectionEnabled = false;


        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusTimeLabel;
        private System.Windows.Forms.Timer displayTimer;
        private Stopwatch stopwatch;


        private void SetupLiveProtection(string pathToCheck)
        {
            try
            {
                liveWatcher = new FileSystemWatcher();
                liveWatcher.Path = pathToCheck;
                liveWatcher.IncludeSubdirectories = true;
                liveWatcher.Created += OnFileCreated;
                liveWatcher.Renamed +=   OnFileCreated;
                liveWatcher.EnableRaisingEvents = true;
                liveWatcher.InternalBufferSize = 65536;

                // Aggiungi un messaggio alla lista risultati
                resultsList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Protezione Real-time attivata su: {pathToCheck}");

                // Aggiorna il fumetto sulla Tray Icon (se presente)
                if (trayIcon != null)
                {
                    trayIcon.BalloonTipTitle = "SalDefender";
                    trayIcon.BalloonTipText = "Protezione Live attivata correttamente.";
                    trayIcon.ShowBalloonTip(3000);
                }
            }
            catch (Exception ex)
            {
                resultsList.Items.Insert(0, "Errore attivazione Live: " + ex.Message);
            }
        }

        private async Task WaitForFileReady(string filePath)
        {

            int attempts = 0;
            while (attempts < 1)
            {
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        resultsList.Items.Insert(0, $"file non bloccato {filePath}");
                        attempts++;
                        return; // Il file è pronto e accessibile
                    }
                }
                catch (IOException ex)
                {
                    // Il file è ancora bloccato, attendi un po' prima di riprovare
                    await Task.Delay(2000);
                    attempts++;
                }
            }
        }


        // 1. Definisci una coda sicura per i thread
        private ConcurrentQueue<string> _filesToScan = new ConcurrentQueue<string>();
        private bool _isProcessing = false;

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            // L'unica cosa che fa l'evento è aggiungere alla coda e uscire subito
            _filesToScan.Enqueue(e.FullPath);
            

            // Avvia il processore se non è già attivo
            if (!_isProcessing)
            {
                Task.Run(() => ProcessQueue());
            }
        }

        private async Task ProcessQueue()
        {
            _isProcessing = true;

            while (_filesToScan.TryDequeue(out string filePath))
            {
                try
                {
                    var clamClient = new ClamClient("localhost", 3310);
                    var scanResult = await clamClient.ScanFileOnServerAsync(filePath);

                    this.Invoke((MethodInvoker)delegate
                    {
                        switch (scanResult.Result)
                        {
                            case ClamScanResults.Clean:
                                resultsList.Items.Insert(0, $"[LIVE] Pulito: {filePath}");
                                break;

                            case ClamScanResults.VirusDetected:
                                // --- AGGIUNTA: Notifica e Suono ---
                                resultsList.Items.Insert(0, $"[!!!] MINACCIA: {filePath} -> {scanResult.RawResult}");

                                // Riproduce il suono di sistema (Beep o Asterisk)
                                System.Media.SystemSounds.Exclamation.Play();

                                // Mostra il fumetto (Balloon Tip) se la trayIcon è configurata
                                if (trayIcon != null)
                                {
                                    trayIcon.ShowBalloonTip(5000, "Virus Rilevato!", $"Minaccia trovata in: {filePath}", ToolTipIcon.Error);
                                }
                                // ----------------------------------
                                break;

                            case ClamScanResults.Error:
                                resultsList.Items.Insert(0, $"[ERRORE] Scansione fallita: {filePath}");
                                break;
                        }
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        resultsList.Items.Insert(0, $"[ERRORE LIVE] {ex.Message}");
                    });
                }
            }
            _isProcessing = false;
        }


        private void LiveProtectionToggle_Click(object sender, EventArgs e)
        {
            if (!isLiveProtectionEnabled)
            {
                // Esempio: monitora la cartella dell'utente
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                SetupLiveProtection(userPath);
                isLiveProtectionEnabled = true;
                resultsList.Items.Insert(0, ">>> Protezione Live ATTIVATA");
            }
            else
            {
                liveWatcher.EnableRaisingEvents = false;
                liveWatcher.Dispose();
                isLiveProtectionEnabled = false;
                resultsList.Items.Insert(0, ">>> Protezione Live DISATTIVATA");
            }
        }

        public MainForm()
        {

            if (Program.IsAlreadyRunning())
            {
                MessageBox.Show("Il programma è già in esecuzione.", "SalDefender", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.Exit(0); // Chiude l'istanza corrente
                return;
            }

            // Creazione della StatusStrip
            statusStrip = new StatusStrip();
            statusTimeLabel = new ToolStripStatusLabel { Text = "Pronto" };
            statusStrip.Items.Add(statusTimeLabel);
            this.Controls.Add(statusStrip); // La aggiunge alla Form

            // Inizializzazione Timer e Stopwatch
            stopwatch = new Stopwatch();
            displayTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            displayTimer.Tick += (s, e) =>
            {
                statusTimeLabel.Text = $"Tempo scansione: {stopwatch.Elapsed:mm\\:ss}";
            };



            // Aggiorna il colore della barra in base al tema
            statusStrip.BackColor = isDarkMode ? DarkPanel : Color.FromKnownColor(KnownColor.Control);
            statusTimeLabel.ForeColor = isDarkMode ? DarkText : LightText;

            this.Text = "SalDefender";
            this.Size = new Size(500, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(500, 650);


            this.FormClosing += MainForm_FormClosing; // Aggiungi questa riga



            // Aggiorna il colore della barra in base al tema
            statusStrip.BackColor = isDarkMode ? DarkPanel : Color.FromKnownColor(KnownColor.Control);
            statusTimeLabel.ForeColor = isDarkMode ? DarkText : LightText;

            InitUI();
        }
        private void StartClamd()
        {
            try
            {
                // 1. Verifica che i processi precedenti siano chiusi per evitare conflitti di porta
                foreach (var proc in Process.GetProcessesByName("clamd"))
                {
                    try { proc.Kill(); } catch { }
                }

                string exePath = @"C:\Program Files\ClamAV\clamd.exe";
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clamd.conf");

                if (!File.Exists(exePath))
                {
                    MessageBox.Show($"File non trovato: {exePath}");
                    return;
                }

                clamdProcess = new Process();
                clamdProcess.StartInfo.FileName = exePath;

                // FONDAMENTALE: Passiamo il percorso config racchiuso tra virgolette doppie escape
                clamdProcess.StartInfo.Arguments = $"--config-file=\"{configPath}\" --foreground";

                clamdProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                clamdProcess.StartInfo.UseShellExecute = true; // Necessario per avere MainWindowHandle
                clamdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                clamdProcess.StartInfo.CreateNoWindow = true; // Nasconde la finestra


                clamdProcess.Start();

                // Aspetta un istante che la finestra venga creata effettivamente dal sistema
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    this.Invoke(new Action(() =>
                    {
                        if (this.WindowState == FormWindowState.Minimized)
                            SetShellTransparency(true);
                    }));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore durante l'avvio di clamd: " + ex.Message);
            }

            VerifyClamdStatus();


        }

        private void VerifyClamdStatus()
        {
            // Cerchiamo i processi attivi con il nome "clamd"
            var processes = Process.GetProcessesByName("clamd");

            while (processes.Length < 0)
            {
                resultsList.Items.Add("Attendere avvio clamd...");
                System.Threading.Thread.Sleep(2000); // Attende mezzo secondo prima di ricontrollare
            }

            var proc = processes[0];
            // Verifichiamo che non sia bloccato o in fase di chiusura
            if (!proc.HasExited)
            {
                resultsList.Items.Add($"CLAMD -> ATTIVO (PID: {proc.Id}, RAM: {proc.WorkingSet64 / 1024 / 1024} MB)");

            }

        }

        private void SetShellTransparency(bool transparent)
        {
            if (clamdProcess == null || clamdProcess.MainWindowHandle == IntPtr.Zero) return;

            IntPtr hWnd = clamdProcess.MainWindowHandle;

            // Imposta lo stile della finestra come "Layered"
            SetWindowLong(hWnd, GWL_EXSTYLE, GetWindowLong(hWnd, GWL_EXSTYLE) | WS_EX_LAYERED);

            // bAlpha: 0 = Completamente trasparente, 255 = Completamente opaco
            byte alpha = transparent ? (byte)0 : (byte)255;
            SetLayeredWindowAttributes(hWnd, 0, alpha, LWA_ALPHA);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Rimuove l'icona dalla tray prima di chiudere (evita icone fantasma)
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }

                // Cerca tutti i processi chiamati "clamd"
                Process[] processes = Process.GetProcessesByName("clamd");

                foreach (Process proc in processes)
                {
                    // Tenta una chiusura gentile prima
                    proc.CloseMainWindow();

                    // Se non si chiude entro 2 secondi, forza il termine
                    if (!proc.WaitForExit(2000))
                    {
                        proc.Kill();
                    }

                    proc.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Opzionale: logga l'errore se necessario
                Debug.WriteLine("Errore durante la chiusura di clamd: " + ex.Message);
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = isDarkMode ? DarkBg : LightBg;
            this.ForeColor = isDarkMode ? DarkText : Color.Black;

            // Aggiorna titolo
            titleLabel.ForeColor = isDarkMode ? Color.White : LightText;

            // Aggiorna controlli specifici
            UpdateControlTheme(this);
        }

        private void UpdateControlTheme(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button btn)
                {
                    btn.BackColor = isDarkMode ? DarkPanel : Color.FromKnownColor(KnownColor.ControlLight);
                    btn.ForeColor = isDarkMode ? Color.White : Color.Black;
                    btn.FlatAppearance.BorderColor = isDarkMode ? Color.Gray : Color.LightGray;
                }
                else if (c is ListBox || c is TextBox || c is ComboBox)
                {
                    c.BackColor = isDarkMode ? Color.FromArgb(60, 60, 60) : Color.White;
                    c.ForeColor = isDarkMode ? Color.White : Color.Black;
                }
                else if (c is GroupBox gb)
                {
                    gb.ForeColor = isDarkMode ? Color.SkyBlue : Color.Black;
                    UpdateControlTheme(gb); // Ricorsivo per i controlli dentro il GroupBox
                }
                else if (c is Panel || c is TableLayoutPanel || c is FlowLayoutPanel)
                {
                    UpdateControlTheme(c); // Ricorsivo
                }
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            // Creazione di un menu contestuale rapido per le impostazioni
            ContextMenuStrip settingsMenu = new ContextMenuStrip();

            var themeToggle = new ToolStripMenuItem(isDarkMode ? "Passa a Tema Chiaro" : "Passa a Tema Scuro");
            themeToggle.Click += (s, args) =>
            {
                isDarkMode = !isDarkMode;
                ApplyTheme();
            };

            settingsMenu.Items.Add(themeToggle);
            settingsMenu.Items.Add(new ToolStripSeparator());
            settingsMenu.Items.Add("Info SalDefender", null, (s, args) => MessageBox.Show("SalDefender v1.0\nProtezione basata su ClamAV", "Informazioni"));

            // Mostra il menu sopra il pulsante
            settingsMenu.Show(settingsButton, new Point(0, settingsButton.Height));
        }




        private void InitUI()
        {
            // --- CONFIGURAZIONE TRAY ICON & ICONA FORM ---
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Apri SalDefender", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Esci", null, (s, e) => Application.Exit());




            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bison_logo-removebg-preview.ico");

            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath, 32, 32);
            }

            trayIcon = new NotifyIcon
            {
                Icon = this.Icon,
                ContextMenuStrip = trayMenu,
                Text = "SalDefender - Protezione Attiva",
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };

            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Hide();
                    trayIcon.ShowBalloonTip(3000, "SalDefender", "L'app è ora attiva in background.", ToolTipIcon.Info);
                }
            };

            // --- CONFIGURAZIONE FORM PRINCIPALE ---
            this.Text = "SalDefender - Security Suite";
            this.MinimumSize = new Size(600, 750); // Leggermente più alto per le nuove opzioni
            this.Padding = new Padding(15);
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.White;


            // 2. Layout Radice
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5
            };
            // All'interno di InitUI()...
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Action Bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200f)); // <--- Alzato a 200f per sicurezza visiva
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Progress
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Results

            this.Controls.Add(mainLayout);

            // --- 0: HEADER SECTION ---
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            SetupHeader(headerPanel);
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // --- 1: ACTION BAR (Solo pulsanti di sistema) ---
            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };

            // Rimosso scanButton da qui
            StyleActionButton(updateButton = new Button { Text = "Aggiorna Firme", Width = 140 }, UpdateButton_Click);
            StyleActionButton(settingsButton = new Button { Text = "Impostazioni", Width = 140 }, SettingsButton_Click);

            actionBar.Controls.AddRange(new Control[] { updateButton, settingsButton });
            mainLayout.Controls.Add(actionBar, 0, 1);

            // --- 2: OPZIONI DI SCANSIONE (Incluso pulsante Cartella) ---
            var optionsPanel = new GroupBox { Text = "Opzioni di Scansione", Dock = DockStyle.Fill, Padding = new Padding(10) };
            SetupScanOptions(optionsPanel);
            mainLayout.Controls.Add(optionsPanel, 0, 2);

            // --- 3: PROGRESS SECTION ---
            var progressPanel = new Panel { Dock = DockStyle.Fill };
            progressLabel = new Label { Text = "Pronto", Dock = DockStyle.Top, Height = 25, ForeColor = Color.DimGray };
            scanProgressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 20, Style = ProgressBarStyle.Continuous };
            progressPanel.Controls.AddRange(new Control[] { progressLabel, scanProgressBar });
            mainLayout.Controls.Add(progressPanel, 0, 3);

            // --- 4: RESULTS ---
            resultsList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                SelectionMode = SelectionMode.MultiExtended
            };
            mainLayout.Controls.Add(resultsList, 0, 4);

            // --- All'interno di InitUI() ---

            // 1. Avvia il processo
            StartClamd();

            // 2. Gestione Riduzione a icona
            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Hide();
                    SetShellTransparency(true); // Rendi la shell invisibile
                }
            };

            // 3. Gestione Riapertura (Tray Icon)
            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                SetShellTransparency(false); // Rendi la shell visibile
            };

            // 4. CHIUSURA TOTALE (Fondamentale!)
            this.FormClosing += (s, e) =>
            {
                if (clamdProcess != null && !clamdProcess.HasExited)
                {
                    try { clamdProcess.Kill(); } catch { }
                }
            };

            // Dentro InitUI()
            liveStatusLabel = new Label();
            liveStatusLabel.Text = "Live: Disattivato";
            liveStatusLabel.ForeColor = Color.Red;
            liveStatusLabel.Location = new Point(350, 200); // Regola la posizione
            mainLayout.Controls.Add(liveStatusLabel);

            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadsPath))
            {
                SetupLiveProtection(downloadsPath);
                // --- SEGNA L'ATTIVAZIONE QUI ---
                liveStatusLabel.Text = "Live: ATTIVO";
                liveStatusLabel.ForeColor = Color.Green;
            }



            LoadDrives();
        }

        public async Task MoveToQuarantineAsync(string originalPath, string quarantineDir, string virusName)
        {
            try
            {
                if (!Directory.Exists(quarantineDir))
                    Directory.CreateDirectory(quarantineDir);

                // Genera un nome file sicuro: data_GUID.quarantine
                string fileId = Guid.NewGuid().ToString("N");
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string newFileName = $"{timestamp}_{fileId}.quarantine";
                string destinationPath = Path.Combine(quarantineDir, newFileName);

                // 1. Sposta il file fisico
                File.Move(originalPath, destinationPath);

                // 2. Crea un file di metadata (Opzionale ma consigliato)
                // Ti permette di sapere cos'era il file originale senza toccare quello infetto
                string metadataPath = destinationPath + ".json";
                string info = $@"{{
            ""OriginalPath"": ""{originalPath.Replace("\\", "\\\\")}"",
            ""Detection"": ""{virusName}"",
            ""Date"": ""{DateTime.Now}""
        }}";

                await File.WriteAllTextAsync(metadataPath, info);

                Console.WriteLine($"[SICUREZZA] File isolato con ID: {fileId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRORE] Impossibile isolare il file: {ex.Message}");
            }
        }

        private void SetScanningMode(bool isScanning)
        {
            // Disabilita i pulsanti di avvio durante la scansione
            diskScanButton.Enabled = !isScanning;
            scanButton.Enabled = !isScanning;
            downloadScanButton.Enabled = !isScanning;
            updateButton.Enabled = !isScanning; // <--- Questo disabilita l'aggiornamento firme

            // Disabilita anche la selezione del disco
            driveComboBox.Enabled = !isScanning;

            // Abilita il tasto Annulla solo se stiamo scansionando
            cancelScanButton.Enabled = isScanning;
        }

        private void SetupScanOptions(GroupBox container)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(5)
            };

            // Proporzioni colonne: 65% input/testo, 35% pulsanti
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            // Altezza uniforme per le 3 righe
            for (int i = 0; i < 3; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));

            // --- Riga 0: URL Download ---
            urlTextBox = new TextBox { PlaceholderText = "URL file...", Dock = DockStyle.Fill, Margin = new Padding(0, 8, 5, 0) };
            downloadScanButton = new Button { Text = "Download & Scan", Dock = DockStyle.Fill, Height = 30 };
            downloadScanButton.Click += DownloadScanButton_Click;

            layout.Controls.Add(urlTextBox, 0, 0);
            layout.Controls.Add(downloadScanButton, 1, 0);

            // --- Riga 1: Disk Scan (Fix per il pulsante Annulla) ---
            driveComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 5, 0) };

            // Usiamo un TableLayoutPanel interno invece di FlowLayoutPanel per precisione millimetrica
            var diskButtonsTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            diskButtonsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            diskButtonsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            diskScanButton = new Button { Text = "Scan Disco", Dock = DockStyle.Fill, Margin = new Padding(2) };
            cancelScanButton = new Button { Text = "Annulla", Dock = DockStyle.Fill, Enabled = false, Margin = new Padding(2) };

            diskScanButton.Click += DiskScanButton_Click;
            cancelScanButton.Click += CancelScanButton_Click;

            diskButtonsTable.Controls.Add(diskScanButton, 0, 0);
            diskButtonsTable.Controls.Add(cancelScanButton, 1, 0);

            layout.Controls.Add(driveComboBox, 0, 1);
            layout.Controls.Add(diskButtonsTable, 1, 1);

            // --- Riga 2: Scansione Cartella ---
            Label folderLabel = new Label { Text = "Analizza una cartella specifica:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            scanButton = new Button { Text = "Sfoglia Cartella", Dock = DockStyle.Fill, Height = 30 };
            scanButton.Click += ScanButton_Click;

            layout.Controls.Add(folderLabel, 0, 2);
            layout.Controls.Add(scanButton, 1, 2);

            container.Controls.Add(layout);
        }
        private void SetupHeader(Panel p)
        {
            logoBox = new PictureBox { Size = new Size(64, 64), Location = new Point(0, 0), SizeMode = PictureBoxSizeMode.Zoom };
            LoadLogoWithIcon();

            titleLabel = new Label
            {
                Text = "SalDefender",
                Font = new Font("Segoe UI Semilight", 22f),
                Location = new Point(75, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(45, 45, 45)
            };
            p.Controls.AddRange(new Control[] { logoBox, titleLabel });
        }

        private void LoadLogoWithIcon()
        {
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bison_logo.png");
            if (File.Exists(logoPath))
            {
                try
                {
                    using (var bmp = new Bitmap(logoPath))
                    {
                        logoBox.Image = new Bitmap(bmp); // Clona per non tenere il file lockato
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = Icon.FromHandle(hIcon);
                        // NOTA: In una app reale, dovresti gestire il DestroyIcon via P/Invoke
                    }
                }
                catch { logoBox.BackColor = Color.GhostWhite; }
            }
        }

        private void StyleActionButton(Button btn, EventHandler handler)
        {
            btn.Height = 35;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.LightGray;
            btn.Cursor = Cursors.Hand;
            btn.Click += handler;
        }

        private void LoadDrives()
        {
            driveComboBox.Items.Clear();
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            foreach (var d in drives) driveComboBox.Items.Add($"{d.Name} ({d.DriveType})");
            if (driveComboBox.Items.Count > 0) driveComboBox.SelectedIndex = 0;
        }

        private async void ScanButton_Click(object sender, EventArgs e)
        {
            resultsList.Items.Clear();
            using var folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() != DialogResult.OK) return;

            string folderPath = folderDialog.SelectedPath;

            // Configurazione inizializzazione come in DiskScan
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            SetUiState(false); // Disabilita i tasti e abilita "Annulla"
            resultsList.Items.Add($"SCANSIONE CARTELLA: {folderPath}");

            scanProgressBar.Value = 0;
            progressLabel.Text = "Preparazione...";

            // Progress reporter per aggiornare la UI in modo thread-safe
            var progress = new Progress<ScanProgress>(p =>
            {
                if (p.Message != null) progressLabel.Text = p.Message;
                scanProgressBar.Value = p.Percent;
                if (p.NewLog != null) resultsList.Items.Add(p.NewLog);

                // Auto-scroll della lista
                if (resultsList.Items.Count > 0)
                    resultsList.TopIndex = resultsList.Items.Count - 1;
            });

            try
            {
                // 1. Recupero file (senza bloccare UI e con supporto annullamento)
                progressLabel.Text = "Conteggio file...";
                var allFiles = await Task.Run(() => SafeGetFiles(folderPath, token), token);

                int totalFiles = allFiles.Count;
                resultsList.Items.Add($"File totali trovati: {totalFiles}");

                if (totalFiles == 0)
                {
                    resultsList.Items.Add("Nessun file trovato nella cartella.");
                    return;
                }

                // 2. Scansione parallela ottimizzata
                int filesScanned = 0;
                int threatsFound = 0;
                var clam = new ClamClient("localhost", 3310);

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8,
                    CancellationToken = token
                };

                await Task.Run(() => Parallel.ForEach(allFiles, parallelOptions, async (file) =>
                {
                    try
                    {
                        // Scansione veloce (invia solo il path)
                        var scanResult = clam.ScanFileOnServerAsync(file).Result;
                        Interlocked.Increment(ref filesScanned);

                        // Calcolo percentuale
                        int pct = (int)((double)filesScanned / totalFiles * 100);

                        if (scanResult.Result == ClamScanResults.VirusDetected)
                        {
                            await MoveToQuarantineAsync(file, @"C:\Quarantine\", scanResult.RawResult);

                            Interlocked.Increment(ref threatsFound);
                            ((IProgress<ScanProgress>)progress).Report(new ScanProgress
                            {
                                NewLog = $"🧨 MINACCIA: {Path.GetFileName(file)} - {scanResult.RawResult}",


                                Percent = pct
                            });
                        }

                        // Aggiorna la label ogni 10 file per non saturare la UI
                        if (filesScanned % 10 == 0 || filesScanned == totalFiles)
                        {
                            ((IProgress<ScanProgress>)progress).Report(new ScanProgress
                            {
                                Message = $"Analisi: {filesScanned}/{totalFiles}",
                                Percent = pct
                            });
                        }
                    }
                    catch (Exception) { }
                }), token);

                resultsList.Items.Add($"--- Scansione completata. Minacce: {threatsFound} ---");
            }
            catch (OperationCanceledException)
            {
                resultsList.Items.Add("⚠️ SCANSIONE ANNULLATA DALL'UTENTE");
                progressLabel.Text = "Annullata";
            }
            catch (Exception ex)
            {
                resultsList.Items.Add($"❌ Errore critico: {ex.Message}");
            }
            finally
            {
                SetUiState(true); // Riabilita l'interfaccia
                SetScanningMode(false);
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        // Funzione SafeGetFiles aggiornata con supporto al CancellationToken
        private List<string> SafeGetFiles(string path, CancellationToken token)
        {
            var files = new List<string>();
            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                if (token.IsCancellationRequested) break;

                string currentDir = stack.Pop();
                try
                {
                    files.AddRange(Directory.GetFiles(currentDir));
                    foreach (string d in Directory.GetDirectories(currentDir))
                    {
                        // Ignora link simbolici (ReparsePoints) per evitare loop infiniti
                        DirectoryInfo di = new DirectoryInfo(d);
                        if ((di.Attributes & FileAttributes.ReparsePoint) == 0)
                            stack.Push(d);
                    }
                }
                catch (UnauthorizedAccessException) { } // Salta cartelle protette
                catch (Exception) { }
            }
            return files;
        }

        private async void DownloadScanButton_Click(object sender, EventArgs e)
        {
            resultsList.Items.Clear();
            string url = urlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                resultsList.Items.Add("Per favore, inserisci un URL valido.");
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) ||
                !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                resultsList.Items.Add("URL non valido. Assicurati che inizi con http:// o https://");
                return;
            }

            resultsList.Items.Add($"Download di: {url}...");
            Application.DoEvents();

            string tempFileName = Path.GetFileName(uriResult.LocalPath);
            if (string.IsNullOrEmpty(tempFileName) || tempFileName.LastIndexOf('.') == -1)
            {
                tempFileName = "downloaded_file_" + DateTime.Now.Ticks.ToString() + ".tmp";
            }
            string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                        {
                            using (var streamToWriteTo = File.Open(tempFilePath, FileMode.Create))
                            {
                                await streamToReadFrom.CopyToAsync(streamToWriteTo);
                            }
                        }
                    }
                }

                resultsList.Items.Add($"Download completato in: {tempFilePath}");
                resultsList.Items.Add("Avvio scansione del file scaricato...");
                Application.DoEvents();

                ClamClient clam = new ClamClient("localhost", 3310);
                var scanResult = await clam.ScanFileOnServerAsync(tempFilePath);

                switch (scanResult.Result)
                {
                    case ClamScanResults.Clean:
                        resultsList.Items.Add($"File scaricato pulito: {tempFilePath}");
                        break;
                    case ClamScanResults.VirusDetected:
                        resultsList.Items.Add($"MINACCIA RILEVATA NEL DOWNLOAD: {tempFilePath} - {scanResult.RawResult}");
                        break;
                    case ClamScanResults.Error:
                        resultsList.Items.Add($"ERRORE durante la scansione del download {tempFilePath}: {scanResult.RawResult}");
                        break;
                    case ClamScanResults.Unknown:
                        resultsList.Items.Add($"Sconosciuto (download): {tempFilePath}");
                        break;
                }
            }
            catch (HttpRequestException httpEx)
            {
                resultsList.Items.Add($"Errore HTTP durante il download: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                resultsList.Items.Add($"Errore generale durante download/scansione: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        resultsList.Items.Add($"File temporaneo rimosso: {tempFilePath}");
                    }
                    catch (Exception ex)
                    {
                        resultsList.Items.Add($"Impossibile rimuovere il file temporaneo {tempFilePath}: {ex.Message}");
                    }
                }
            }
        }

        private async void UpdateButton_Click(object sender, EventArgs e)
        {
            updateButton.Enabled = false;
            scanButton.Enabled = false;
            downloadScanButton.Enabled = false;
            diskScanButton.Enabled = false;

            resultsList.Items.Clear();
            resultsList.Items.Add("Avvio aggiornamento firme (freshclam)...");
            progressLabel.Text = "Aggiornamento in corso...";
            Application.DoEvents();

            string freshclamPath = @"C:\Program Files\ClamAV\freshclam.exe";
            string configPath = @"c:\Users\Avangarde\.gemini\antigravity\brain\32811d22-ea56-4491-97c9-c2b605c08512\SalDefender\freshclam.conf";

            if (!File.Exists(freshclamPath))
            {
                resultsList.Items.Add("ERRORE: freshclam.exe non trovato in C:\\Program Files\\ClamAV\\");
                updateButton.Enabled = true;
                scanButton.Enabled = true;
                downloadScanButton.Enabled = true;
                diskScanButton.Enabled = true;
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = freshclamPath,
                        Arguments = $"--config-file=\"{configPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(psi))
                    {
                        process.OutputDataReceived += (s, args) =>
                        {
                            if (args.Data != null)
                                this.Invoke(new Action(() =>
                                {
                                    resultsList.Items.Add(args.Data);
                                    resultsList.TopIndex = resultsList.Items.Count - 1;
                                }));
                        };
                        process.ErrorDataReceived += (s, args) =>
                        {
                            if (args.Data != null)
                                this.Invoke(new Action(() =>
                                {
                                    resultsList.Items.Add("ERR: " + args.Data);
                                    resultsList.TopIndex = resultsList.Items.Count - 1;
                                }));
                        };

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                    }
                });

                resultsList.Items.Add("Processo di aggiornamento terminato.");
                progressLabel.Text = "Aggiornamento completato.";
            }
            catch (Exception ex)
            {
                resultsList.Items.Add($"Errore esecuzione freshclam: {ex.Message}");
                progressLabel.Text = "Errore aggiornamento.";
            }
            finally
            {
                updateButton.Enabled = true;
                scanButton.Enabled = true;
                downloadScanButton.Enabled = true;
                diskScanButton.Enabled = true;
            }
        }


        private async void DiskScanButton_Click(object sender, EventArgs e)
        {
            if (driveComboBox.SelectedItem == null)
            {
                MessageBox.Show("Seleziona un disco.", "SalDefender", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }



            resultsList.Items.Clear();
            string drivePath = driveComboBox.SelectedItem.ToString().Substring(0, 3);
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // UI Setup
            SetUiState(false); // Funzione helper per disabilitare bottoni
            SetScanningMode(true);
            resultsList.Items.Add($"AVVIO SCANSIONE DISCO: {drivePath}");

            var progress = new Progress<ScanProgress>(p =>
            {
                progressLabel.Text = p.Message;
                scanProgressBar.Value = p.Percent;
                if (p.NewLog != null) resultsList.Items.Add(p.NewLog);
                if (resultsList.Items.Count > 100) resultsList.TopIndex = resultsList.Items.Count - 1;
            });

            try
            {
                // 1. Recupero file (senza bloccare UI)
                ((IProgress<ScanProgress>)progress).Report(new ScanProgress { Message = "Indicizzazione file..." });
                var allFiles = await Task.Run(() => GetAllFilesRecursive(drivePath, token));
                int totalFiles = allFiles.Count;
                resultsList.Items.Add($"File trovati: {totalFiles}");

                // 2. Scansione Parallela (il vero boost)
                int filesScanned = 0;
                int threatsFound = 0;
                var clam = new ClamClient("localhost", 3310);

                // Limitiamo il parallelismo per non saturare i thread di ClamAV (configurati prima)
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8,
                    CancellationToken = token
                };

                await Task.Run(() => Parallel.ForEach(allFiles, parallelOptions, (file) =>
                {
                    try
                    {
                        // Scansione veloce via Path
                        var scanResult = clam.ScanFileOnServerAsync(file).Result;
                        Interlocked.Increment(ref filesScanned);

                        if (scanResult.Result == ClamScanResults.VirusDetected)
                        {
                            Interlocked.Increment(ref threatsFound);
                            ((IProgress<ScanProgress>)progress).Report(new ScanProgress
                            {
                                NewLog = $"🧨 MINACCIA: {Path.GetFileName(file)} - {scanResult.RawResult}",
                                Percent = (int)((double)filesScanned / totalFiles * 100)
                            });
                        }

                        // Aggiorna progresso ogni 50 file per non sovraccaricare la UI
                        if (filesScanned % 50 == 0)
                        {
                            ((IProgress<ScanProgress>)progress).Report(new ScanProgress
                            {
                                Message = $"Analizzati: {filesScanned}/{totalFiles}",
                                Percent = (int)((double)filesScanned / totalFiles * 100)
                            });
                        }
                    }
                    catch { /* Gestione errori silenziosa per file inaccessibili */ }
                }));

                resultsList.Items.Add($"Scansione completata. Minacce: {threatsFound}");
            }
            catch (OperationCanceledException) { resultsList.Items.Add("⚠️ Scansione interrotta dall'utente."); }
            catch (Exception ex) { MessageBox.Show($"Errore: {ex.Message}"); }
            finally { SetUiState(true); SetScanningMode(false); }
        }

        private void SetUiState(bool isReady)
        {
            // Se isReady è true, la scansione è finita/pronta (Abilita i controlli)
            // Se isReady è false, la scansione è in corso (Disabilita i controlli)

            // Pulsanti di avvio scansione
            diskScanButton.Enabled = isReady;
            scanButton.Enabled = isReady;
            downloadScanButton.Enabled = isReady;
            driveComboBox.Enabled = isReady;

            // Pulsante di annullamento (attivo solo se la scansione è in corso)
            cancelScanButton.Enabled = !isReady;

            // Feedback visivo sulla ProgressBar
            if (isReady)
            {
                // Se vuoi resettare dopo la fine, scommenta la riga sotto
                // scanProgressBar.Value = 0; 
                progressLabel.ForeColor = Color.Black;
            }
            else
            {
                progressLabel.ForeColor = Color.DarkBlue;
                resultsList.Items.Add("------------------------------------------");
            }

            // Forza l'aggiornamento immediato della UI
            this.Refresh();
        }

        // Classe di supporto per il progresso
        public class ScanProgress
        {
            public string Message { get; set; }
            public int Percent { get; set; }
            public string NewLog { get; set; }
        }

        private List<string> GetAllFilesRecursive(string path, CancellationToken token)
        {
            List<string> files = new List<string>();
            try
            {
                try
                {
                    files.AddRange(Directory.GetFiles(path));
                }
                catch { }

                try
                {
                    foreach (string directory in Directory.GetDirectories(path))
                    {
                        if (token.IsCancellationRequested)
                            break;
                        files.AddRange(GetAllFilesRecursive(directory, token));
                    }
                }
                catch { }
            }
            catch { }
            return files;
        }

        private void CancelScanButton_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                progressLabel.Text = "Annullamento in corso...";
                cancelScanButton.Enabled = false;
            }
        }


        public void EnableWeeklyScan(string time, string day)
        {
            // Esempio: time = "09:00", day = "MON"
            string exePath = AppDomain.CurrentDomain.BaseDirectory + "SalDefender.exe";
            string cmd = $"/Create /SC WEEKLY /D {day} /TN \"SalDefenderScan\" /TR \"'{exePath}' --silent-scan\" /ST {time} /F";

            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = cmd,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        public void DisableWeeklyScan()
        {
            string cmd = "/Delete /TN \"SalDefenderScan\" /F";

            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = cmd,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

    }
}
