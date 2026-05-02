@echo off
REM Run this ONCE as Administrator on the Windows host before launching the API.
REM
REM 1) Reserves http://+:80/ so the OWIN self-host can listen on all interfaces
REM    without admin rights at runtime.
REM 2) Opens TCP 80 in Windows Firewall so external traffic (Cloudflare) can reach it.
REM
REM Change the URL/port below if you don't want to use port 80.

set URL=http://+:80/
set PORT=80

echo Reserving URL ACL for %URL% ...
netsh http add urlacl url=%URL% user=Everyone

echo Opening Windows Firewall for TCP %PORT% ...
netsh advfirewall firewall add rule name="Blauberg API (%PORT%)" dir=in action=allow protocol=TCP localport=%PORT%

echo.
echo Done. You can now run blaubergselector-wrapper-coils.exe (no admin needed).
pause
