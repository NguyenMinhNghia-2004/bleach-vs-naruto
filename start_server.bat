@echo off
title BLEACH VS NARUTO - ONLINE RELAY SERVER
echo =========================================================
echo    DANG KHOI CHAY MAY CHU RELAY BLEACH VS NARUTO ONLINE
echo =========================================================

if not exist "%~dp0tools\cloudflared.exe" (
    echo Dang tai cloudflared.exe ve thu muc tools...
    powershell -Command "Invoke-WebRequest -Uri 'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe' -OutFile '%~dp0tools\cloudflared.exe'"
)

start "BVN Relay Server (TCP 7777 & WS 7778)" "%~dp0Build\Server\BleachVsNarutoServer.exe" 7777
timeout /t 2 /nobreak >nul
start "Cloudflare Public Tunnel" "%~dp0tools\cloudflared.exe" tunnel --url http://127.0.0.1:7778

echo.
echo May chu da duoc kich hoat!
echo - Ban PC Client: Ket noi qua TCP cong 7777
echo - Ban Web Browser: Ket noi qua WebSocket cong 7778
echo - Public URL tu Cloudflare Tunnel dang hien thi tren cua so Cloudflare.
echo.
pause
