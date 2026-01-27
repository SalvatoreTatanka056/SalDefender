import os

code = """
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

        private void InitUI()
        {
            logoBox = new PictureBox();
            logoBox.Size = new Size(64, 64);
            logoBox.Location = new Point(20, 20);
            logoBox.BackColor = Color.LightGray;
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
        }

        private async void ScanButton_Click(object sender, EventArgs e)
        {
            resultsList.Items.Clear();
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Seleziona la cartella da scansionare";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;
                    resultsList.Items.Add($"Scansione di: {folderPath}");

                    ClamClient clam = new ClamClient("localhost", 3310);
                    int found = 0;

                    try
                    {
                        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            resultsList.Items.Add($"Analisi: {file}...");
                            Application.DoEvents(); 

                            var scanResult = await clam.ScanFileOnServerAsync(file);

                            switch (scanResult.Result)
                            {
                                case ClamScanResults.Clean:
                                    break;
                                case ClamScanResults.VirusDetected:
                                    resultsList.Items.Add($"MINACCIA RILEVATA: {file} - {scanResult.RawResult}");
                                    found++;
                                    break;
                                case ClamScanResults.Error:
                                    resultsList.Items.Add($"ERRORE durante la scansione di {file}: {scanResult.RawResult}");
                                    break;
                                case ClamScanResults.Unknown:
                                    resultsList.Items.Add($"Sconosciuto: {file}");
                                    break;
                            }
                        }

                        if (found == 0)
                            resultsList.Items.Add("Scansione completata: Nessuna minaccia rilevata.");
                        else
                            resultsList.Items.Add($"Scansione completata: {found} minaccia(e) rilevata(e).");
                    }
                    catch (Exception ex)
                    {
                        resultsList.Items.Add($"Errore generale durante la scansione: {ex.Message}");
                    }
                }
                else
                {
                    resultsList.Items.Add("Scansione annullata.");
                }
            }
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

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Funzione di aggiornamento non ancora implementata.", "SalDefender");
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Impostazioni non ancora disponibili.", "SalDefender");
        }

        private async void DiskScanButton_Click(object sender, EventArgs e)
        {
            if (driveComboBox.SelectedItem == null)
            {
                MessageBox.Show("Seleziona un disco da scansionare.", "SalDefender", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            resultsList.Items.Clear();
            
            string selectedDrive = driveComboBox.SelectedItem.ToString();
            string drivePath = selectedDrive.Substring(0, 3); 

            resultsList.Items.Add("SCANSIONE DISCO COMPLETA: " + drivePath);
            resultsList.Items.Add("");
            
            cancellationTokenSource = new CancellationTokenSource();
            
            diskScanButton.Enabled = false;
            scanButton.Enabled = false;
            downloadScanButton.Enabled = false;
            cancelScanButton.Enabled = true;
            
            scanProgressBar.Value = 0;
            progressLabel.Text = "Preparazione scansione...";

            ClamClient clam = new ClamClient("localhost", 3310);
            int filesScanned = 0;
            int threatsFound = 0;
            int errors = 0;

            try
            {
                progressLabel.Text = "Conteggio file in corso...";
                Application.DoEvents();

                List<string> allFiles = new List<string>();
                await Task.Run(() =>
                {
                    try
                    {
                        allFiles = GetAllFilesRecursive(drivePath, cancellationTokenSource.Token);
                    }
                    catch
                    {
                    }
                }, cancellationTokenSource.Token);

                int totalFiles = allFiles.Count;
                resultsList.Items.Add($"File totali da scansionare: {totalFiles}");
                resultsList.Items.Add("");

                foreach (var file in allFiles)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        resultsList.Items.Add("");
                        resultsList.Items.Add("SCANSIONE ANNULLATA DALL'UTENTE");
                        break;
                    }

                    filesScanned++;
                    
                    if (filesScanned % 10 == 0 || filesScanned == 1)
                    {
                        progressLabel.Text = $"File: {filesScanned}/{totalFiles} - {Path.GetFileName(file)}";
                        scanProgressBar.Value = Math.Min(100, (int)((double)filesScanned / totalFiles * 100));
                        Application.DoEvents();
                    }

                    try
                    {
                        var scanResult = await clam.ScanFileOnServerAsync(file);

                        switch (scanResult.Result)
                        {
                            case ClamScanResults.Clean:
                                break;
                            case ClamScanResults.VirusDetected:
                                resultsList.Items.Add($"MINACCIA: {file}");
                                resultsList.Items.Add($"   Tipo: {scanResult.RawResult}");
                                threatsFound++;
                                break;
                            case ClamScanResults.Error:
                                errors++;
                                if (errors <= 10) 
                                {
                                    resultsList.Items.Add($"ERRORE: {file}");
                                }
                                break;
                        }

                        if (resultsList.Items.Count > 0)
                            resultsList.TopIndex = resultsList.Items.Count - 1;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        errors++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        if (errors <= 10)
                        {
                            resultsList.Items.Add($"Eccezione su {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                }

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    resultsList.Items.Add("");
                    resultsList.Items.Add($"File scansionati: {filesScanned}");
                    resultsList.Items.Add($"Minacce rilevate: {threatsFound}");
                    
                    if (threatsFound == 0)
                    {
                        resultsList.Items.Add("SISTEMA PULITO!");
                    }
                    else
                    {
                        resultsList.Items.Add("ATTENZIONE: Minacce rilevate!");
                    }

                    progressLabel.Text = $"Completato: {filesScanned} file, {threatsFound} minacce";
                    scanProgressBar.Value = 100;
                }
            }
            catch (OperationCanceledException)
            {
                resultsList.Items.Add("Scansione annullata.");
                progressLabel.Text = "Scansione annullata";
            }
            catch (Exception ex)
            {
                resultsList.Items.Add($"Errore critico durante la scansione disco: {ex.Message}");
                progressLabel.Text = "Errore durante la scansione";
            }
            finally
            {
                diskScanButton.Enabled = true;
                scanButton.Enabled = true;
                downloadScanButton.Enabled = true;
                cancelScanButton.Enabled = false;
                
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
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
"""
with open('MainForm.cs', 'w', encoding='utf-8') as f:
    f.write(code)
