@echo off
title ByteClick Cloudflare Tunnel
color 0A
echo 🚀 Cloudflare Tunnel Baslatiliyor...
echo ---------------------------------------------------
echo 💡 NOT: Linki kopyalayip TradingView'e yapistirmayi unutma!
echo 💡 Siyah pencereyi kapatirsan baglanti kopar.
echo ---------------------------------------------------
echo.

:: Tüneli başlatır ve linki ekranda tutar
cloudflared tunnel --url http://localhost:7021

echo.
echo ⚠️ Tunel kapandi! Yeniden baslatmak icin bir tusa basin...
pause