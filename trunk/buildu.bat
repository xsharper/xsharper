set path=C:\WINDOWS\Microsoft.NET\Framework\v3.5\;%PATH%
xsharper masterBuild.xsh  //genexe masterBuild.exe //debugc

masterBuild.exe //debugc /fromBuildBat /upload
del masterBuild.exe
