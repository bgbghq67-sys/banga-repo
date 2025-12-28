@echo off
%~d0
cd %~dp0
@echo on
@if /I "%PROCESSOR_ARCHITECTURE%" == "AMD64" (
  REM echo Installing driver from 64-bit installer on 64-bit OS
  start DriverPackage\dpinst64.exe
) else if /I "%PROCESSOR_ARCHITECTURE%" == "X86" (
  @if /I "%PROCESSOR_ARCHITEW6432%" == "AMD64" (
    REM echo Installing driver from 32-bit installer on 64-bit OS (WOW64)
    start DriverPackage\dpinst64.exe
  ) else (
    REM echo Installing driver from 32-bit installer on 32-bit OS
    start DriverPackage\dpinst32.exe
  )
)
