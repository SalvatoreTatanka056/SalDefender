@echo off
echo Starting ClamAV Daemon...
"C:\Program Files\ClamAV\clamd.exe" -c "%~dp0clamd.conf"
pause
