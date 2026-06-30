@ECHO OFF
REM  QBFC Project Options Begin
REM  HasVersionInfo: No
REM Companyname: 
REM Productname: 
REM Filedescription: 
REM Copyrights: 
REM Trademarks: 
REM Originalname: 
REM Comments: 
REM Productversion:  0. 0. 0. 0
REM Fileversion:  0. 0. 0. 0
REM Internalname: 
REM ExeType: console
REM Architecture: x64
REM Appicon: 
REM AdministratorManifest: No
REM  QBFC Project Options End
@ECHO ON
@ECHO OFF
REM  QBFC Project Options Begin
REM  HasVersionInfo: No
REM Companyname:
REM Productname:
REM Filedescription:
REM Copyrights:
REM Trademarks:
REM Originalname:
REM Comments:
REM Productversion:  0. 0. 0. 0
REM Fileversion:  0. 0. 0. 0
REM Internalname:
REM ExeType: console
REM Architecture: x64
REM Appicon:
REM AdministratorManifest: No
REM  QBFC Project Options End
@ECHO ON
@echo off
title ByteClick Dual Tunnel and Mailer
color 0A

:: CMD ekranýný UTF-8 moduna alýyoruz
chcp 65001 >nul

:: ==========================================
::      GUZEL HOSTING SMTP EMAIL SETTINGS
:: ==========================================
set "GONDERICI_MAIL=info@cephemodelleme.com"
set "MAIL_SIFRESI=87f@6ogG7"
set "ALICI_MAIL=karamankarani42@gmail.com"
set "SMTP_SUNUCU=mt-chocolate-win.guzelhosting.com"
set "SMTP_PORT=587"
:: ==========================================

echo BTClick Cloudflare Tunnels Starting...
echo ---------------------------------------------------
echo NOTE: Do not close this window or tunnels will drop.
echo ---------------------------------------------------
echo.

:: Eski geçici log dosyalarýný temizle
if exist "%temp%\tlog1.txt" del "%temp%\tlog1.txt"
if exist "%temp%\tlog2.txt" del "%temp%\tlog2.txt"

echo Tunnel services are launching...
start /b "" cloudflared tunnel --url http://localhost:7021 > "%temp%\tlog1.txt" 2>&1
timeout /t 3 /nobreak >nul
start /b "" cloudflared tunnel --url http://localhost:8080 > "%temp%\tlog2.txt" 2>&1

echo Scanning logs for active links...

:: 30 saniyelik sayaç ve link kontrol döngüsü (Tamamen Batch tabanlý)
set /a counter=0
set "LINK1="
set "LINK2="

:loop
if %counter% geq 30 goto timeout_error

:: Log 1 kontrolü
if exist "%temp%\tlog1.txt" (
    for /f "tokens=*" %%a in ('findstr /r "https://[a-zA-Z0-9-]*\.trycloudflare\.com" "%temp%\tlog1.txt"') do (
        for %%b in (%%a) do (
            echo %%b | findstr "trycloudflare.com" >nul && set "LINK1=%%b"
        )
    )
)

:: Log 2 kontrolü
if exist "%temp%\tlog2.txt" (
    for /f "tokens=*" %%a in ('findstr /r "https://[a-zA-Z0-9-]*\.trycloudflare\.com" "%temp%\tlog2.txt"') do (
        for %%b in (%%a) do (
            echo %%b | findstr "trycloudflare.com" >nul && set "LINK2=%%b"
        )
    )
)

:: Ýki link de bulundu mu?
if not "%LINK1%"=="" if not "%LINK2%"=="" goto send_mail

timeout /t 1 /nobreak >nul
set /a counter+=1
goto loop

:send_mail
echo.
echo  LINK: %LINK1%

:: Mail baþarýlý olduðunda Write-Host çýktýsý kaldýrýldý.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$smtp = New-Object Net.Mail.SmtpClient('%SMTP_SUNUCU%', %SMTP_PORT%); $smtp.EnableSsl = $true; $smtp.Credentials = New-Object Net.NetworkCredential('%GONDERICI_MAIL%', '%MAIL_SIFRESI%'); $msg = New-Object Net.Mail.MailMessage; $msg.From = '%GONDERICI_MAIL%'; $msg.To.Add('%ALICI_MAIL%'); $msg.Subject = 'ByteClick Live Tunnel Links'; $msg.Body = '<h3>ByteClick Tunnels Successfully Opened!</h3><p>Active TradingView Webhook endpoints:</p><ul><li><a href=\"%LINK1%\">%LINK1%</a></li><li><a href=\"%LINK2%\">%LINK2%</a></li></ul>'; $msg.IsBodyHtml = $true; try { $smtp.Send($msg); } catch { Write-Host 'Email Error' -ForegroundColor Red; }"
goto stream_logs

:timeout_error
echo ?? Timeout: Tunnel links could not be fetched within 30 seconds.
goto stream_logs

:stream_logs
echo.
echo ---------------------------------------------------
echo Live Tunnel Streams (Press Ctrl+C to exit)
echo ---------------------------------------------------
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-Content '%temp%\tlog1.txt','%temp%\tlog2.txt' -Wait -Tail 5"
