# SalDefender

SalDefender è un antivirus demo sviluppato in C# (Windows Forms) con integrazione ClamAV.

## Funzionalità

### 1. Scansione Cartella
- Seleziona una cartella tramite dialog
- Scansiona ricorsivamente tutti i file
- Mostra risultati in tempo reale

### 2. Download e Scansione URL
- Inserisci un URL HTTP/HTTPS
- Scarica il file in una directory temporanea
- Scansiona automaticamente il file scaricato
- Rimuove il file temporaneo dopo la scansione

### 3. **NUOVO: Scansione Disco Completa**
- Seleziona un disco dal menu a tendina (C:\, D:\, ecc.)
- Scansiona ricorsivamente tutti i file del disco
- **Barra di progresso** con percentuale completamento
- **Contatore file** scansionati in tempo reale
- **Pulsante Annulla** per interrompere la scansione
- Gestione errori migliorata per file protetti
- Riepilogo finale con statistiche complete

## Requisiti

1. **ClamAV** installato e in esecuzione
2. Il demone ClamAV (clamd) deve essere accessibile su localhost:3310
3. **.NET 8.0** o superiore

## Come eseguire

1. Assicurati di avere ClamAV installato e il suo demone (clamd) in esecuzione
2. Apri il progetto in Visual Studio o usa la CLI:
   `ash
   dotnet restore
   dotnet build
   dotnet run
   `

## Architettura

- **MainForm.cs**: Interfaccia grafica principale
- **Program.cs**: Entry point dell'applicazione
- **nClam**: Libreria per integrazione con ClamAV

## Miglioramenti Implementati

✅ Scansione disco completa con selezione drive
✅ Progress bar per feedback visivo
✅ Cancellazione asincrona delle scansioni
✅ Gestione robusta degli errori (file protetti, permessi)
✅ UI responsive durante scansioni lunghe
✅ Auto-scroll dei risultati
✅ Conteggio file totali prima della scansione

## Note

- Il logo è un placeholder (sfondo grigio)
- La funzione "Aggiorna Firme" è un segnaposto
- La funzione "Impostazioni" è un segnaposto
- La scansione disco può richiedere molto tempo su dischi grandi
- I file protetti dal sistema vengono ignorati silenziosamente

## Licenza

Demo/Educational purposes

C:\Program Files\ClamAV\clamd.exe`" -c `"c:\Users\Avangarde\.gemini\antigravity\brain\32811d22-ea56-4491-97c9-c2b605c08512\SalDefender\clamd.conf`" --foreground`r`n`