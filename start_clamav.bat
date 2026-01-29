@echo off
:: Avvia ClamAV in background
start "" "C:\Program Files\ClamAV\clamd.exe" -c "%~dp0clamd.conf" --foreground

:: Avvia SalDefender
start "" "C:\Users\Avangarde\.gemini\antigravity\brain\32811d22-ea56-4491-97c9-c2b605c08512\SalDefender\bin\Debug\net8.0-windows\SalDefender.exe"

:: Chiude immediatamente la finestra della shell
exit