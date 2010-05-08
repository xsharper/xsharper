@echo off
set path=C:\WINDOWS\Microsoft.NET\Framework\v3.5\;%PATH%
xsharper masterBuild.xsh  //genexe masterBuild.exe //debugc

if %ERRORLEVEL% EQU 0 goto continue
if %ERRORLEVEL% EQU 9009 echo. && echo Please download XSharper from http://xsharper.com/xsharper.exe to build  && goto quit
echo.
echo Build failed

:continue

masterBuild.exe //debugc /fromBuildBat %*
del masterBuild.exe

:quit