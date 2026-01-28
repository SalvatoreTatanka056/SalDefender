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

namespace SalDefender
{
    public class MainForm : Form
    {
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

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public MainForm()
        {
            this.Text = "SalDefender";
            this.Size = new Size(500, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(500, 650);

            InitUI();
        }

        /*   private void InitUI()
           {
               logoBox = new PictureBox();
               logoBox.Size = new Size(64, 64);
               logoBox.Location = new Point(20, 20);
               logoBox.SizeMode = PictureBoxSizeMode.Zoom;
               logoBox.BackColor = Color.Transparent;

               string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bison_logo.png");
               if (File.Exists(logoPath))
               {
                   try
                   {
                       Bitmap bmp = new Bitmap(logoPath);
                       logoBox.Image = bmp;
                       IntPtr hIcon = bmp.GetHicon();
                       this.Icon = Icon.FromHandle(hIcon);
                   }
                   catch
                   {
                       logoBox.BackColor = Color.LightGray;
                   }
               }
               else
               {
                   logoBox.BackColor = Color.LightGray;
               }

               this.Controls.Add(logoBox);

               titleLabel = new Label();
               titleLabel.Text = "SalDefender";
               titleLabel.Font = new Font("Segoe UI", 20, FontStyle.Bold);
               titleLabel.Location = new Point(100, 30);
               titleLabel.AutoSize = true;
               this.Controls.Add(titleLabel);

               scanButton = new Button();
               scanButton.Text = "Scansiona Cartella"; 
               scanButton.Location = new Point(20, 100);
               scanButton.Size = new Size(120, 40);
               scanButton.Click += ScanButton_Click;
               this.Controls.Add(scanButton);

               updateButton = new Button();
               updateButton.Text = "Aggiorna Firme";
               updateButton.Location = new Point(160, 100);
               updateButton.Size = new Size(120, 40);
               updateButton.Click += UpdateButton_Click;
               this.Controls.Add(updateButton);

               settingsButton = new Button();
               settingsButton.Text = "Impostazioni";
               settingsButton.Location = new Point(300, 100);
               settingsButton.Size = new Size(120, 40);
               settingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
               settingsButton.Click += SettingsButton_Click;
               this.Controls.Add(settingsButton);

               urlTextBox = new TextBox();
               urlTextBox.PlaceholderText = "Inserisci URL del file da scaricare e scansionare";
               urlTextBox.Location = new Point(20, 155);
               urlTextBox.Size = new Size(300, 25);
               urlTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
               this.Controls.Add(urlTextBox);

               downloadScanButton = new Button();
               downloadScanButton.Text = "Download & Scansiona";
               downloadScanButton.Location = new Point(325, 150);
               downloadScanButton.Size = new Size(135, 35);
               downloadScanButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
               downloadScanButton.Click += DownloadScanButton_Click;
               this.Controls.Add(downloadScanButton);

               Label diskScanLabel = new Label();
               diskScanLabel.Text = "Scansione Disco Completa:";
               diskScanLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
               diskScanLabel.Location = new Point(20, 200);
               diskScanLabel.AutoSize = true;
               this.Controls.Add(diskScanLabel);

               driveComboBox = new ComboBox();
               driveComboBox.Location = new Point(20, 225);
               driveComboBox.Size = new Size(100, 25);
               driveComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

               foreach (DriveInfo drive in DriveInfo.GetDrives())
               {
                   if (drive.IsReady)
                   {
                       driveComboBox.Items.Add($"{drive.Name} ({drive.DriveType})");
                   }
               }
               if (driveComboBox.Items.Count > 0)
                   driveComboBox.SelectedIndex = 0;

               this.Controls.Add(driveComboBox);

               diskScanButton = new Button();
               diskScanButton.Text = "Scansiona Disco";
               diskScanButton.Location = new Point(130, 220);
               diskScanButton.Size = new Size(120, 35);
               diskScanButton.Click += DiskScanButton_Click;
               this.Controls.Add(diskScanButton);

               cancelScanButton = new Button();
               cancelScanButton.Text = "Annulla";
               cancelScanButton.Location = new Point(260, 220);
               cancelScanButton.Size = new Size(80, 35);
               cancelScanButton.Click += CancelScanButton_Click;
               cancelScanButton.Enabled = false;
               this.Controls.Add(cancelScanButton);

               progressLabel = new Label();
               progressLabel.Text = "Pronto";
               progressLabel.Location = new Point(20, 265);
               progressLabel.Size = new Size(440, 20);
               progressLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
               progressLabel.ForeColor = Color.DarkBlue;
               this.Controls.Add(progressLabel);

               scanProgressBar = new ProgressBar();
               scanProgressBar.Location = new Point(20, 290);
               scanProgressBar.Size = new Size(440, 25);
               scanProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
               scanProgressBar.Style = ProgressBarStyle.Continuous;
               this.Controls.Add(scanProgressBar);

               resultsList = new ListBox();
               resultsList.Location = new Point(20, 325);
               resultsList.Size = new Size(440, 280);
               resultsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
               this.Controls.Add(resultsList);
           }*/

        private void InitUI()
        {

                        // Configurazione Menu Contestuale della Tray Icon
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Apri SalDefender", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("-"); // Separatore
            trayMenu.Items.Add("Esci", null, (s, e) => Application.Exit());

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bison_logo.ico");

            if (File.Exists(iconPath))
            {
                // Carica l'icona direttamente dal file
                this.Icon = new Icon(iconPath);
            }

            // Configurazione della NotifyIcon
            trayIcon = new NotifyIcon {
                Icon = this.Icon, // Usa la stessa icona del form
                ContextMenuStrip = trayMenu,
                Text = "SalDefender - Protezione Attiva",
                Visible = true
            };

        

            // Evento per riaprire con doppio click sulla tray icon
            trayIcon.DoubleClick += (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };

            // Gestione della riduzione a icona
            this.Resize += (s, e) => {
                if (this.WindowState == FormWindowState.Minimized) {
                    this.Hide(); // Nasconde l'app dalla barra delle applicazioni
                    trayIcon.ShowBalloonTip(3000, "SalDefender", "L'app è ora attiva in background.", ToolTipIcon.Info);
                }
            };

            // 1. Configurazione Form Principale
            this.Text = "SalDefender - Security Suite";
            this.MinimumSize = new Size(600, 700);
            this.Padding = new Padding(15);
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.White;

            // 2. Layout Radice (TableLayoutPanel per fluidità)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Actions
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f)); // Scan Options
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Progress
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Results List

            this.Controls.Add(mainLayout);

            // --- HEADER SECTION ---
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            SetupHeader(headerPanel);
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // --- BUTTON BAR (Action Bar) ---
            //var actionBar = new FlowLayoutPanel { Dock = DockStyle.Fill, Gaps = new Padding(0, 10, 0, 0) };
            // CORREZIONE 1: FlowLayoutPanel non ha "Gaps", usa Padding
            var actionBar = new FlowLayoutPanel { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(0, 10, 0, 0) // Spazio interno
            };

            StyleActionButton(scanButton = new Button { Text = "Scansiona Cartella", Width = 140 }, ScanButton_Click);
            StyleActionButton(updateButton = new Button { Text = "Aggiorna Firme", Width = 140 }, UpdateButton_Click);
            StyleActionButton(settingsButton = new Button { Text = "Impostazioni", Width = 140 }, SettingsButton_Click);

            actionBar.Controls.AddRange(new Control[] { scanButton, updateButton, settingsButton });
            mainLayout.Controls.Add(actionBar, 0, 1);

            // --- SCAN OPTIONS (URL & Disk) ---
            var optionsPanel = new GroupBox { Text = "Opzioni di Scansione", Dock = DockStyle.Fill, Padding = new Padding(10) };
            SetupScanOptions(optionsPanel);
            mainLayout.Controls.Add(optionsPanel, 0, 2);

            // --- PROGRESS SECTION ---
            var progressPanel = new Panel { Dock = DockStyle.Fill };
            progressLabel = new Label { Text = "Pronto", Dock = DockStyle.Top, Height = 25, ForeColor = Color.DimGray };
            scanProgressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 20, Style = ProgressBarStyle.Continuous };
            progressPanel.Controls.AddRange(new Control[] { progressLabel, scanProgressBar });
            mainLayout.Controls.Add(progressPanel, 0, 3);

            // --- RESULTS ---
            resultsList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                SelectionMode = SelectionMode.MultiExtended
            };
            mainLayout.Controls.Add(resultsList, 0, 4);

            LoadDrives();
        }

        // CORREZIONE 2: Aggiungi il metodo SetupScanOptions che mancava
        private void SetupScanOptions(GroupBox container)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

            // Riga 1: URL Download
            urlTextBox = new TextBox { 
                PlaceholderText = "URL file...", 
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 5, 5)
            };
            downloadScanButton = new Button { 
                Text = "Download & Scan", 
                Dock = DockStyle.Fill,
                Height = 30 
            };
            downloadScanButton.Click += DownloadScanButton_Click;

            // Riga 2: Disk Scan
            driveComboBox = new ComboBox { 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 5, 5)
            };
            
            var diskBtnLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            diskScanButton = new Button { Text = "Scan Disco", Width = 90 };
            cancelScanButton = new Button { Text = "Annulla", Width = 70, Enabled = false };
            
            diskScanButton.Click += DiskScanButton_Click;
            cancelScanButton.Click += CancelScanButton_Click;

            diskBtnLayout.Controls.AddRange(new Control[] { diskScanButton, cancelScanButton });

            // Aggiunta al layout
            layout.Controls.Add(urlTextBox, 0, 0);
            layout.Controls.Add(downloadScanButton, 1, 0);
            layout.Controls.Add(driveComboBox, 0, 1);
            layout.Controls.Add(diskBtnLayout, 1, 1);

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

                await Task.Run(() => Parallel.ForEach(allFiles, parallelOptions, (file) =>
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
                    catch (Exception) { /* Salta file che ClamAV non riesce a leggere al momento */ }
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

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Impostazioni non ancora disponibili.", "SalDefender");
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
            catch (OperationCanceledException) { resultsList.Items.Add("⚠️ Scansione interrotta."); }
            catch (Exception ex) { MessageBox.Show($"Errore: {ex.Message}"); }
            finally { SetUiState(true); }
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
    }
}
