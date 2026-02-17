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
    /// <summary>
    /// Classe statica per la configurazione centrale dell'applicazione
    /// </summary>
    internal static class AppConfig
    {
        // Percorsi ClamAV
        public static readonly string ClamAVPath = @"C:\Program Files\ClamAV\clamd.exe";
        public static readonly string FreshclamPath = @"C:\Program Files\ClamAV\freshclam.exe";
        
        // Percorsi quarantena e configurazione
        public static readonly string QuarantineDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SalDefender", "Quarantine");
        
        public static readonly int ClamAVPort = 3310;
        public static readonly string ClamAVHost = "localhost";

        /// <summary>
        /// Valida che ClamAV sia disponibile sul sistema
        /// </summary>
        public static bool ValidateClamAVInstallation()
        {
            return File.Exists(ClamAVPath) && File.Exists(FreshclamPath);
        }

        /// <summary>
        /// Inizializza le directory necessarie
        /// </summary>
        public static void InitializeDirectories()
        {
            if (!Directory.Exists(QuarantineDirectory))
                Directory.CreateDirectory(QuarantineDirectory);
        }
    }

    public class MainForm : Form
    {

        private bool isDarkMode = false;

        // Definizione Palette
        private readonly Color DarkBg = Color.FromArgb(32, 32, 32);
        private readonly Color DarkPanel = Color.FromArgb(45, 45, 45);
        private readonly Color DarkText = Color.WhiteSmoke;
        private readonly Color LightBg = Color.White;
        private readonly Color LightText = Color.FromArgb(45, 45, 45);
        private Button scanButton = null!;
        private Button updateButton = null!;
        private Button settingsButton = null!;
        private ListBox resultsList = null!;
        private Label titleLabel = null!;
        private PictureBox logoBox = null!;
        private TextBox urlTextBox = null!;
        private Button downloadScanButton = null!;
        private Button diskScanButton = null!;
        private ComboBox driveComboBox = null!;
        private ProgressBar scanProgressBar = null!;
        private Label progressLabel = null!;
        private Button cancelScanButton = null!;
        private CancellationTokenSource? cancellationTokenSource;

        // Dichiarazione in alto con le altre variabili
        private Label liveStatusLabel = null!;

        private NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;
        private Process? clamdProcess; // Questa è la variabile che mancava

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_ALPHA = 0x2;

        private List<FileSystemWatcher> liveWatchers = new List<FileSystemWatcher>();
        private bool isLiveProtectionEnabled = false;


        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel statusTimeLabel = null!;
        private System.Windows.Forms.Timer displayTimer = null!;
        private Stopwatch stopwatch = null!;


        private void SetupLiveProtection(string pathToCheck)
        {
            try
            {
                Debug.WriteLine($"[LIVE] SetupLiveProtection called per: {pathToCheck}");
                Debug.WriteLine($"[LIVE] Directory exists: {Directory.Exists(pathToCheck)}");

                var watcher = new FileSystemWatcher();
                watcher.Path = pathToCheck;
                watcher.IncludeSubdirectories = true;
                
                // Monitora tutti i tipi di evento
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                
                watcher.Created += OnFileCreated;
                watcher.Renamed += OnFileRenamed;
                watcher.Changed += OnFileChanged;
                
                // Error handler per il FileSystemWatcher
                watcher.Error += (s, e) =>
                {
                    Debug.WriteLine($"[LIVE] FileSystemWatcher ERROR: {e.GetException()?.Message}");
                };
                
                watcher.InternalBufferSize = 131072;
                watcher.EnableRaisingEvents = true;
                
                // Aggiunta alla lista di watchers
                liveWatchers.Add(watcher);
                
                Debug.WriteLine($"[LIVE] FileSystemWatcher ATTIVATO su {pathToCheck}");

                // Aggiungi un messaggio alla lista risultati
                this.Invoke((MethodInvoker)delegate
                {
                    resultsList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Protezione Real-time attivata su: {pathToCheck}");

                    // Aggiorna il fumetto sulla Tray Icon (se presente)
                    if (trayIcon != null)
                    {
                        trayIcon.BalloonTipTitle = "SalDefender";
                        trayIcon.BalloonTipText = "Protezione Live attivata correttamente.";
                        trayIcon.ShowBalloonTip(3000);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LIVE] Errore SetupLiveProtection: {ex.Message}\n{ex.StackTrace}");
                
                this.Invoke((MethodInvoker)delegate
                {
                    resultsList.Items.Insert(0, "Errore attivazione Live: " + ex.Message);
                });
            }
        }

        private void SetupMultipleLiveProtection()
        {
            try
            {
                // Pulisci vecchi watchers
                foreach (var watcher in liveWatchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                liveWatchers.Clear();
                Debug.WriteLine($"[LIVE] Tutti i watchers precedenti eliminati");

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var pathsToMonitor = new List<string>
                {
                    Path.Combine(userProfile, "Downloads"),
                    Path.Combine(userProfile, "Documents"),
                    Path.Combine(userProfile, "Desktop"),
                    userProfile // Monitora anche la radice del profilo per file critici
                };

                int activatedCount = 0;
                foreach (var path in pathsToMonitor)
                {
                    if (Directory.Exists(path))
                    {
                        SetupLiveProtection(path);
                        activatedCount++;
                    }
                }

                isLiveProtectionEnabled = true;
                _liveProtectionRunning = true;
                
                // Avvia il ProcessQueue se non è già attivo
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    Debug.WriteLine($"[LIVE] Avvio ProcessQueue dal setup");
                    Task.Run(() => ProcessQueue());
                }
                
                // Aggiorna lo status dopo aver attivato tutti
                if (liveStatusLabel != null)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        liveStatusLabel.Text = $"Live: ATTIVO ({activatedCount} cartelle)";
                        liveStatusLabel.ForeColor = Color.Green;
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LIVE] Errore SetupMultipleLiveProtection: {ex.Message}");
            }
        }




        // Coda sicura per i thread con deduplicazione e throttling
        private ConcurrentQueue<string> _filesToScan = new ConcurrentQueue<string>();
        private HashSet<string> _filesInQueue = new HashSet<string>();
        private SemaphoreSlim _liveScanLimiter = new SemaphoreSlim(2); // MAX 2 scansioni live simultanee per non bloccare altri scan
        private object _queueLock = new object();
        private volatile bool _isProcessing = false; // volatile per visibilità tra thread
        private ManualResetEvent _fileArrivedEvent = new ManualResetEvent(false); // Segnala nuovi file
        private volatile bool _liveProtectionRunning = false; // Controlla se il processore deve stare attivo

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                string filePath = e.FullPath;
                Debug.WriteLine($"[LIVE-Created] Evento ricevuto: {Path.GetFileName(filePath)}");
                
                EnqueueFileForScanning(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in OnFileCreated: {ex.Message}");
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                string filePath = e.FullPath;
                Debug.WriteLine($"[LIVE-Renamed] File rinominato: {e.OldName} -> {Path.GetFileName(filePath)}");
                
                EnqueueFileForScanning(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in OnFileRenamed: {ex.Message}");
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                string filePath = e.FullPath;
                // Solo per file regolari, non directory
                if (!File.Exists(filePath))
                    return;

                Debug.WriteLine($"[LIVE-Changed] File modificato: {Path.GetFileName(filePath)}");
                
                EnqueueFileForScanning(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in OnFileChanged: {ex.Message}");
            }
        }

        private void EnqueueFileForScanning(string filePath)
        {
            try
            {
                // Evita duplicati nella coda
                lock (_queueLock)
                {
                    if (_filesInQueue.Contains(filePath))
                    {
                        Debug.WriteLine($"[LIVE] File già in coda, ignorato: {filePath}");
                        return;
                    }
                    
                    _filesInQueue.Add(filePath);
                }
                
                _filesToScan.Enqueue(filePath);
                Debug.WriteLine($"[LIVE] File aggiunto alla coda: {Path.GetFileName(filePath)}");

                // Segnala che un nuovo file è arrivato
                _fileArrivedEvent.Set();

                // Avvia il processore se non è già attivo (doppio check)
                if (!_isProcessing && _liveProtectionRunning)
                {
                    _isProcessing = true;
                    Debug.WriteLine($"[LIVE] Avvio ProcessQueue");
                    Task.Run(() => ProcessQueue());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LIVE] Errore EnqueueFileForScanning: {ex.Message}");
            }
        }

        private async Task ProcessQueue()
        {
            try
            {
                // Loop continuo che rimane attivo finché la protezione live è attiva
                while (_liveProtectionRunning)
                {
                    // Tenta di estrarre un file dalla coda
                    if (_filesToScan.TryDequeue(out string? filePath))
                    {
                        if (string.IsNullOrEmpty(filePath)) continue;
                        
                        Debug.WriteLine($"[LIVE] Processamento file: {filePath}");
                        
                        // Rimuovi dalla lista di deduplica
                        lock (_queueLock)
                        {
                            _filesInQueue.Remove(filePath);
                        }

                        bool semaphoreAcquired = false;
                        try
                        {
                            // Attendi che il file sia completamente scritto (max 5 secondi)
                            bool isReady = await WaitForFileReadyQuick(filePath, 5000);
                            
                            if (!isReady)
                            {
                                Debug.WriteLine($"[LIVE] File non divenuto ready, saltato: {filePath}");
                                continue; // File bloccato o rimosso, salta
                            }

                            Debug.WriteLine($"[LIVE] File ready, acquisendo semaforo: {filePath}");

                            // IMPORTANTE: Acquisisci il semaforo SOLO se il file è ready
                            await _liveScanLimiter.WaitAsync();
                            semaphoreAcquired = true;
                            
                            Debug.WriteLine($"[LIVE] Semaforo acquisito, scanning: {filePath}");

                            try
                            {
                                var clamClient = new ClamClient(AppConfig.ClamAVHost, AppConfig.ClamAVPort);
                                var scanResult = await clamClient.ScanFileOnServerAsync(filePath).ConfigureAwait(false);
                                
                                Debug.WriteLine($"[LIVE] Scan completato: {Path.GetFileName(filePath)} -> {scanResult.Result}");

                                this.Invoke((MethodInvoker)delegate
                                {
                                    try
                                    {
                                        switch (scanResult.Result)
                                        {
                                            case ClamScanResults.Clean:
                                                resultsList.Items.Insert(0, $"<Live> File Pulito: {Path.GetFileName(filePath)}");                                            
                                                break;

                                            case ClamScanResults.VirusDetected:
                                                resultsList.Items.Insert(0, $"[!!!] MINACCIA: {Path.GetFileName(filePath)} - {scanResult.RawResult}");
                                                System.Media.SystemSounds.Exclamation.Play();
                                                if (trayIcon != null)
                                                    trayIcon.ShowBalloonTip(5000, "⚠️ VIRUS RILEVATO!", 
                                                        $"Minaccia: {Path.GetFileName(filePath)}", ToolTipIcon.Error);
                                                
                                                // Metti in quarantena
                                                _ = MoveToQuarantineAsync(filePath, AppConfig.QuarantineDirectory, scanResult.RawResult);
                                                break;

                                            case ClamScanResults.Error:
                                                Debug.WriteLine($"[LIVE] Errore scan: {filePath}");
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[LIVE] Errore UI: {ex.Message}");
                                    }
                                });
                            }
                            catch (TaskCanceledException)
                            {
                                Debug.WriteLine($"[LIVE] Timeout scan: {filePath}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[LIVE] Errore scansione: {ex.Message}");
                            }
                        }
                        finally
                        {
                            // Rilascia il semaforo SOLO se è stato acquisito
                            if (semaphoreAcquired)
                            {
                                _liveScanLimiter.Release();
                                Debug.WriteLine($"[LIVE] Semaforo rilasciato");
                            }
                            
                            await Task.Delay(50); // Piccola pausa per non sovraccaricare
                        }

                        // Reset dell'evento se la coda è vuota, in modo da attendere nuovi file
                        if (_filesToScan.IsEmpty)
                        {
                            _fileArrivedEvent.Reset();
                        }
                    }
                    else
                    {
                        // Coda vuota: attendi che arrivi un nuovo file (max 5 secondi) oppure termina
                        Debug.WriteLine($"[LIVE] Coda vuota, attesa di nuovi file...");
                        _fileArrivedEvent.Reset();
                        
                        // Attendi segnale di nuovo file oppure timeout
                        bool signaled = _fileArrivedEvent.WaitOne(5000);
                        
                        if (!signaled)
                        {
                            Debug.WriteLine($"[LIVE] Timeout attesa file, processore in sleep");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LIVE] Errore ProcessQueue: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                Debug.WriteLine($"[LIVE] ProcessQueue terminato - Protezione Live: {(_liveProtectionRunning ? "ATTIVA" : "INATTIVA")}");
            }
        }

        // Versione veloce con retry migliorato
        private async Task<bool> WaitForFileReadyQuick(string filePath, int timeoutMs)
        {
            int elapsed = 0;
            const int checkInterval = 200;

            while (elapsed < timeoutMs)
            {
                try
                {
                    // Controlla se il file esiste ancora
                    if (!File.Exists(filePath))
                    {
                        Debug.WriteLine($"[LIVE] File non trovato: {filePath}");
                        return false;
                    }

                    // Usa FileShare.ReadWrite (più permissivo) per permettere accesso durante la scrittura
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Debug.WriteLine($"[LIVE] File ready: {Path.GetFileName(filePath)}");
                        return true;
                    }
                }
                catch (FileNotFoundException)
                {
                    Debug.WriteLine($"[LIVE] FileNotFound: {filePath}");
                    return false;
                }
                catch (IOException)
                {
                    elapsed += checkInterval;
                    Debug.WriteLine($"[LIVE] Retry {elapsed}ms: {Path.GetFileName(filePath)} - IO locked");
                    
                    if (elapsed < timeoutMs)
                        await Task.Delay(checkInterval);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LIVE] Unexpected error: {ex.Message}");
                    return false;
                }
            }

            Debug.WriteLine($"[LIVE] Timeout raggiunto per: {Path.GetFileName(filePath)}");
            return false;
        }


        private void LiveProtectionToggle_Click(object sender, EventArgs e)
        {
            if (!isLiveProtectionEnabled)
            {
                SetupMultipleLiveProtection();
                resultsList.Items.Insert(0, ">>> Protezione Live ATTIVATA");
            }
            else
            {
                _liveProtectionRunning = false; // Segnala al ProcessQueue di terminare
                
                foreach (var watcher in liveWatchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                liveWatchers.Clear();
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

            // Inizializza le directory necessarie (quarantena, etc.)
            AppConfig.InitializeDirectories();

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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Validazione ClamAV
            if (!AppConfig.ValidateClamAVInstallation())
            {
                MessageBox.Show(
                    "Avviso: ClamAV non sembra essere installato correttamente.\n\n" +
                    "Percorsi cercati:\n" +
                    $"- {AppConfig.ClamAVPath}\n" +
                    $"- {AppConfig.FreshclamPath}\n\n" +
                    "Scarica e installa ClamAV da: https://www.clamav.net/",
                    "SalDefender - Configurazione ClamAV",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            
            // Setup live protection after form handle is created
            SetupMultipleLiveProtection();
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

                string exePath = AppConfig.ClamAVPath;
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clamd.conf");

                if (!File.Exists(exePath))
                {
                    MessageBox.Show($"ClamAV non trovato in: {exePath}\nAssicurati che ClamAV sia installato in C:\\Program Files\\ClamAV\\");
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

            int retries = 0;
            while (processes.Length == 0 && retries < 10)
            {
                resultsList.Items.Add("Attendere avvio clamd...");
                System.Threading.Thread.Sleep(500); // Attende 500ms prima di ricontrollare
                processes = Process.GetProcessesByName("clamd");
                retries++;
            }

            if (processes.Length > 0)
            {
                var proc = processes[0];
                // Verifichiamo che non sia bloccato o in fase di chiusura
                if (!proc.HasExited)
                {
                    resultsList.Items.Add($"CLAMD -> ATTIVO (PID: {proc.Id}, RAM: {proc.WorkingSet64 / 1024 / 1024} MB)");
                }
            }
            else
            {
                resultsList.Items.Add("⚠️ AVVISO: ClamAV (clamd) non riuscito ad avviarsi.");
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

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {

            // Impedisce la chiusura tramite il pulsante X (disabilita la X)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                // Disattiva la protezione live in modo ordinato
                _liveProtectionRunning = false;
                
                // Disabilita tutti i live watchers
                foreach (var watcher in liveWatchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                liveWatchers.Clear();

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

        private void SettingsButton_Click(object? sender, EventArgs e)
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
            StyleActionButton(updateButton = new Button { Text = "Aggiorna Firme", Width = 140 }, UpdateButton_Click!);
            StyleActionButton(settingsButton = new Button { Text = "Impostazioni", Width = 140 }, SettingsButton_Click!);

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

                // Prova a spostare il file con retry per file bloccati
                int moveAttempts = 0;
                bool movingSucceeded = false;
                while (moveAttempts < 3 && !movingSucceeded)
                {
                    try
                    {
                        File.Move(originalPath, destinationPath, overwrite: true);
                        movingSucceeded = true;
                        Debug.WriteLine($"[QUARANTINE] File spostato: {originalPath} -> {destinationPath}");
                    }
                    catch (IOException) when (moveAttempts < 2)
                    {
                        moveAttempts++;
                        await Task.Delay(500);
                    }
                }

                if (movingSucceeded)
                {
                    // Crea un file di metadata
                    string metadataPath = destinationPath + ".json";
                    string info = $@"{{
    ""OriginalPath"": ""{originalPath.Replace("\\", "\\\\")}"",
    ""Detection"": ""{virusName}"",
    ""Date"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"",
    ""FileSize"": {(File.Exists(originalPath) ? new FileInfo(originalPath).Length : 0)}
}}";

                    try
                    {
                        await File.WriteAllTextAsync(metadataPath, info);
                    }
                    catch (Exception metaEx)
                    {
                        Debug.WriteLine($"[QUARANTINE] Avvertimento: metadata non scritto - {metaEx.Message}");
                    }

                    Debug.WriteLine($"[QUARANTINE] File isolato con ID: {fileId}");
                }
                else
                {
                    Debug.WriteLine($"[QUARANTINE] ERRORE: Impossibile spostare {originalPath} dopo 3 tentativi");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QUARANTINE] Errore critico: {ex.Message}\n{ex.StackTrace}");
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
            downloadScanButton.Click += DownloadScanButton_Click!;

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

            diskScanButton.Click += DiskScanButton_Click!;
            cancelScanButton.Click += CancelScanButton_Click!;

            diskButtonsTable.Controls.Add(diskScanButton, 0, 0);
            diskButtonsTable.Controls.Add(cancelScanButton, 1, 0);

            layout.Controls.Add(driveComboBox, 0, 1);
            layout.Controls.Add(diskButtonsTable, 1, 1);

            // --- Riga 2: Scansione Cartella ---
            Label folderLabel = new Label { Text = "Analizza una cartella specifica:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            scanButton = new Button { Text = "Sfoglia Cartella", Dock = DockStyle.Fill, Height = 30 };
            scanButton.Click += ScanButton_Click!;

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

        private void ScanButton_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() != DialogResult.OK) return;

            string folderPath = folderDialog.SelectedPath;

            // Avvia la scansione su un thread separato
            Task.Run(() => PerformFolderScan(folderPath));
        }

        private async Task PerformFolderScan(string folderPath)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    resultsList.Items.Clear();
                    resultsList.Items.Add($"SCANSIONE CARTELLA: {folderPath}");
                    scanProgressBar.Value = 0;
                    progressLabel.Text = "Preparazione...";
                    SetUiState(false);
                });

                // Configurazione cancellazione
                cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;

                // Progress reporter per aggiornare la UI in modo thread-safe
                var progress = new Progress<ScanProgress>(p =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (p.Message != null) progressLabel.Text = p.Message;
                        scanProgressBar.Value = p.Percent;
                        if (p.NewLog != null) resultsList.Items.Add(p.NewLog);

                        // Auto-scroll della lista
                        if (resultsList.Items.Count > 0)
                            resultsList.TopIndex = resultsList.Items.Count - 1;
                    });
                });

                // 1. Recupero file in background
                ((IProgress<ScanProgress>)progress).Report(new ScanProgress { Message = "Conteggio file..." });
                var allFiles = await Task.Run(() => SafeGetFiles(folderPath, token), token);

                int totalFiles = allFiles.Count;
                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"File totali trovati: {totalFiles}"); });

                if (totalFiles == 0)
                {
                    this.Invoke((MethodInvoker)delegate { resultsList.Items.Add("Nessun file trovato nella cartella."); });
                    return;
                }

                // 2. Scansione parallela ottimizzata
                int filesScanned = 0;
                int threatsFound = 0;
                var clamClient = new ClamClient(AppConfig.ClamAVHost, AppConfig.ClamAVPort);

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8,
                    CancellationToken = token
                };

                await Task.Run(() => Parallel.ForEach(allFiles, parallelOptions, (file) =>
                {
                    try
                    {
                        if (token.IsCancellationRequested) return;

                        // Scansione veloce via Path
                        var scanResult = clamClient.ScanFileOnServerAsync(file).Result;
                        Interlocked.Increment(ref filesScanned);

                        // Calcolo percentuale
                        int pct = (int)((double)filesScanned / totalFiles * 100);

                        if (scanResult.Result == ClamScanResults.VirusDetected)
                        {
                            Interlocked.Increment(ref threatsFound);
                            _ = MoveToQuarantineAsync(file, AppConfig.QuarantineDirectory, scanResult.RawResult);

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

                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"--- Scansione completata. Minacce: {threatsFound} ---"); });
            }
            catch (OperationCanceledException)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    resultsList.Items.Add("⚠️ SCANSIONE ANNULLATA DALL'UTENTE");
                    progressLabel.Text = "Annullata";
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"❌ Errore critico: {ex.Message}"); });
            }
            finally
            {
                this.Invoke((MethodInvoker)delegate
                {
                    SetUiState(true);
                    SetScanningMode(false);
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null!;
                });

                // Riavvia il ProcessQueue della live protection se era attivo
                // (potrebbe essersi bloccato durante la scansione della cartella)
                if (_liveProtectionRunning && !_isProcessing && _filesToScan.Count > 0)
                {
                    _isProcessing = true;
                    Debug.WriteLine($"[LIVE] Riavvio ProcessQueue dopo scansione cartella");
                    Task.Run(() => ProcessQueue());
                }
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

        private void DownloadScanButton_Click(object? sender, EventArgs e)
        {
            string url = urlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                resultsList.Items.Add("Per favore, inserisci un URL valido.");
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) ||
                !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                resultsList.Items.Add("URL non valido. Assicurati che inizi con http:// o https://");
                return;
            }

            // Avvia lo scaricamento e la scansione su un thread separato
            Task.Run(() => PerformDownloadAndScan(url, uriResult));
        }

        private async Task PerformDownloadAndScan(string url, Uri uriResult)
        {
            this.Invoke((MethodInvoker)delegate { resultsList.Items.Clear(); });

            string tempFileName = Path.GetFileName(uriResult.LocalPath);
            if (string.IsNullOrEmpty(tempFileName) || tempFileName.LastIndexOf('.') == -1)
            {
                tempFileName = "downloaded_file_" + DateTime.Now.Ticks.ToString() + ".tmp";
            }
            string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

            try
            {
                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"Download di: {url}..."); });

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

                this.Invoke((MethodInvoker)delegate
                {
                    resultsList?.Items.Add($"Download completato in: {tempFilePath}");
                    resultsList?.Items.Add("Avvio scansione del file scaricato...");
                });

                ClamClient clam = new ClamClient(AppConfig.ClamAVHost, AppConfig.ClamAVPort);
                var scanResult = await clam.ScanFileOnServerAsync(tempFilePath);

                this.Invoke((MethodInvoker)delegate
                {
                    switch (scanResult.Result)
                    {
                        case ClamScanResults.Clean:
                            resultsList?.Items.Add($"File scaricato pulito: {tempFilePath}");
                            break;
                        case ClamScanResults.VirusDetected:
                            resultsList?.Items.Add($"MINACCIA RILEVATA NEL DOWNLOAD: {tempFilePath} - {scanResult.RawResult}");
                            System.Media.SystemSounds.Exclamation.Play();
                            break;
                        case ClamScanResults.Error:
                            resultsList?.Items.Add($"ERRORE durante la scansione del download {tempFilePath}: {scanResult.RawResult}");
                            break;
                        case ClamScanResults.Unknown:
                            resultsList?.Items.Add($"Sconosciuto (download): {tempFilePath}");
                            break;
                    }
                });
            }
            catch (HttpRequestException httpEx)
            {
                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"Errore HTTP durante il download: {httpEx.Message}"); });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"Errore generale durante download/scansione: {ex.Message}"); });
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"File temporaneo rimosso: {tempFilePath}"); });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"Impossibile rimuovere il file temporaneo {tempFilePath}: {ex.Message}"); });
                    }
                }
            }
        }

        private async void UpdateButton_Click(object? sender, EventArgs e)
        {
            updateButton.Enabled = false;
            scanButton.Enabled = false;
            downloadScanButton.Enabled = false;
            diskScanButton.Enabled = false;

            resultsList.Items.Clear();
            resultsList.Items.Add("=== Aggiornamento Firme ClamAV ===");
            progressLabel.Text = "Aggiornamento in corso...";
            Application.DoEvents();

            string freshclamPath = AppConfig.FreshclamPath;
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "freshclam.conf");

            // Validazioni preliminari
            resultsList.Items.Add($"🔍 Verifica percorsi...");
            resultsList.Items.Add($"   freshclam.exe: {freshclamPath}");
            resultsList.Items.Add($"   freshclam.conf: {configPath}");

            if (!File.Exists(freshclamPath))
            {
                resultsList.Items.Add($"❌ ERRORE: freshclam.exe non trovato in {freshclamPath}");
                resultsList.Items.Add("   Assicurati che ClamAV sia installato in C:\\Program Files\\ClamAV\\");
                updateButton.Enabled = true;
                scanButton.Enabled = true;
                downloadScanButton.Enabled = true;
                diskScanButton.Enabled = true;
                return;
            }

            if (!File.Exists(configPath))
            {
                resultsList.Items.Add($"❌ ERRORE: File config non trovato: {configPath}");
                updateButton.Enabled = true;
                scanButton.Enabled = true;
                downloadScanButton.Enabled = true;
                diskScanButton.Enabled = true;
                return;
            }

            resultsList.Items.Add("✓ File trovati, avvio aggiornamento...");

            try
            {
                // Assicurati che le directory di destinazione esistano
                try
                {
                    string dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clamav_db");
                    string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clamav_logs");
                    
                    if (!Directory.Exists(dbDir))
                    {
                        Directory.CreateDirectory(dbDir);
                        resultsList.Items.Add($"✓ Creata cartella database: {dbDir}");
                    }
                    
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                        resultsList.Items.Add($"✓ Creata cartella log: {logDir}");
                    }
                }
                catch (Exception dirEx)
                {
                    resultsList.Items.Add($"⚠️ Avvertimento creazione cartelle: {dirEx.Message}");
                }

                resultsList.Items.Add("");
                resultsList.Items.Add("📥 Output di freshclam:");
                resultsList.Items.Add("─────────────────────────────────");

                await Task.Run(() =>
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = freshclamPath,
                        // Usiamo il percorso assoluto per il config file
                        Arguments = $"--config-file=\"{Path.GetFullPath(configPath)}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                    };

                    int exitCode = -1;
                    using (Process? process = Process.Start(psi))
                    {
                        if (process == null)
                        {
                            this.Invoke(new Action(() =>
                            {
                                resultsList.Items.Add("❌ ERRORE: Impossibile avviare freshclam");
                            }));
                            return;
                        }

                        process.OutputDataReceived += (s, args) =>
                        {
                            if (args.Data != null)
                                this.Invoke(new Action(() =>
                                {
                                    resultsList.Items.Add(args.Data);
                                    if (resultsList.Items.Count > 0)
                                        resultsList.TopIndex = resultsList.Items.Count - 1;
                                }));
                        };

                        process.ErrorDataReceived += (s, args) =>
                        {
                            if (args.Data != null)
                                this.Invoke(new Action(() =>
                                {
                                    resultsList.Items.Add("⚠️ " + args.Data);
                                    if (resultsList.Items.Count > 0)
                                        resultsList.TopIndex = resultsList.Items.Count - 1;
                                }));
                        };

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit(120000); // Timeout 2 minuti
                        exitCode = process.ExitCode;
                    }

                    this.Invoke(new Action(() =>
                    {
                        resultsList.Items.Add("─────────────────────────────────");
                        
                        if (exitCode == 0)
                        {
                            resultsList.Items.Add("✅ Aggiornamento completato con successo!");
                            progressLabel.Text = "Aggiornamento completato.";
                        }
                        else
                        {
                            resultsList.Items.Add($"⚠️ Aggiornamento terminato con codice: {exitCode}");
                            resultsList.Items.Add($"   (0=successo, 1=update disponibili, >1=errore)");
                            progressLabel.Text = $"Aggiornamento terminato (codice: {exitCode})";
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                resultsList.Items.Add($"❌ Errore critico: {ex.GetType().Name}");
                resultsList.Items.Add($"   {ex.Message}");
                if (ex.InnerException != null)
                    resultsList.Items.Add($"   Dettaglio: {ex.InnerException.Message}");
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


        private void DiskScanButton_Click(object? sender, EventArgs e)
        {
            if (driveComboBox.SelectedItem == null)
            {
                MessageBox.Show("Seleziona un disco.", "SalDefender", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string drivePath = driveComboBox.SelectedItem.ToString()?.Substring(0, 3) ?? "C:\\";

            // Avvia la scansione su un thread separato
            Task.Run(() => PerformDiskScan(drivePath));
        }

        private async Task PerformDiskScan(string drivePath)
        {
            try
            {
                // Configurazione UI iniziale da thread UI
                this.Invoke((MethodInvoker)delegate
                {
                    resultsList.Items.Clear();
                    resultsList.Items.Add($"AVVIO SCANSIONE DISCO: {drivePath}");
                    SetUiState(false);
                    SetScanningMode(true);
                });

                cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;

                var progress = new Progress<ScanProgress>(p =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (p.Message != null) progressLabel.Text = p.Message;
                        scanProgressBar.Value = p.Percent;
                        if (p.NewLog != null) resultsList.Items.Add(p.NewLog);
                        if (resultsList.Items.Count > 100) resultsList.TopIndex = resultsList.Items.Count - 1;
                    });
                });

                // 1. Recupero file in background
                ((IProgress<ScanProgress>)progress).Report(new ScanProgress { Message = "Indicizzazione file..." });
                var allFiles = await Task.Run(() => GetAllFilesRecursive(drivePath, token), token);
                int totalFiles = allFiles.Count;
                
                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"File trovati: {totalFiles}"); });

                // 2. Scansione Parallela
                int filesScanned = 0;
                int threatsFound = 0;
                var clam = new ClamClient(AppConfig.ClamAVHost, AppConfig.ClamAVPort);

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8,
                    CancellationToken = token
                };

                await Task.Run(() => Parallel.ForEach(allFiles, parallelOptions, (file) =>
                {
                    try
                    {
                        if (token.IsCancellationRequested) return;

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

                        // Aggiorna progresso ogni 50 file
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
                }), token);

                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add($"Scansione completata. Minacce: {threatsFound}"); });
            }
            catch (OperationCanceledException)
            {
                this.Invoke((MethodInvoker)delegate { resultsList.Items.Add("⚠️ Scansione interrotta dall'utente."); });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate { MessageBox.Show($"Errore: {ex.Message}"); });
            }
            finally
            {
                this.Invoke((MethodInvoker)delegate
                {
                    SetUiState(true);
                    SetScanningMode(false);
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null!;
                });

                // Riavvia il ProcessQueue della live protection se era attivo
                // (potrebbe essersi bloccato durante la scansione del disco)
                if (_liveProtectionRunning && !_isProcessing && _filesToScan.Count > 0)
                {
                    _isProcessing = true;
                    Debug.WriteLine($"[LIVE] Riavvio ProcessQueue dopo scansione disco");
                    Task.Run(() => ProcessQueue());
                }
            }
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
            public string? Message { get; set; }
            public int Percent { get; set; }
            public string? NewLog { get; set; }
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

        private void CancelScanButton_Click(object? sender, EventArgs e)
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
