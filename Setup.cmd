@echo off
if not exist "%~dp0scripts\setup.ps1" (
  echo Setup can't find its files. Extract the ZIP first: right-click it, choose Extract All,
  echo then run Setup.cmd from the extracted folder.
  echo.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\setup.ps1" %*
echo.
pause
