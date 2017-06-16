@echo off
set f=UDIMAS
set rbin=%~dp0..\UDIMAS\bin\Release
set bf=%~dp0%f%
set srcdir=%~dp0..\

echo Building %f%..

rmdir /S /Q %bf%
mkdir %bf%

:: Main files
copy /B /Y %rbin%\UDIMAS.exe %bf%\
copy /Y %rbin%\UDIMAS.exe.config %bf%\
copy /Y %rbin%\log.config %bf%\

:: Dependencies
copy /B /Y %rbin%\IronPython.dll %bf%\
copy /B /Y %rbin%\IronPython.Modules.dll %bf%\
copy /B /Y %rbin%\IronPython.SQLite.dll %bf%\
copy /B /Y %rbin%\IronPython.Wpf.dll %bf%\
copy /B /Y %rbin%\log4net.dll %bf%\
copy /B /Y %rbin%\Microsoft.Dynamic.dll %bf%\
copy /B /Y %rbin%\Microsoft.Scripting.dll %bf%\
copy /B /Y %rbin%\Microsoft.Scripting.Metadata.dll %bf%\
copy /B /Y %rbin%\Newtonsoft.Json.dll %bf%\
copy /B /Y %rbin%\System.Collections.Immutable.dll %bf%\

:: Plugins
mkdir %bf%\plugins\
copy /B /Y %srcdir%\UDINet\bin\Release\UDINet.exe %bf%\plugins\
copy /B /Y %srcdir%\UDINet\bin\Release\Scs.dll %bf%\plugins\
copy /B /Y %srcdir%\Discorder\bin\Release\Discorder.dll %bf%\plugins\

echo Build completed into '%f%' folder

:: Zipping
echo Zipping folder %f%..
del "%bf%.zip" 2>nul
call zip "%bf%" "%bf%.zip"
echo Removing %f%..
rmdir /S /Q %bf%
