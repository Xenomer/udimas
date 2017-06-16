@echo off
set f=UDINet_Standalone

echo Building %f%..

mkdir %~dp0%f%
copy /Y %~dp0..\UDINet\bin\Release\UDINet.exe %~dp0\%f%\UDINet.exe
copy /Y %~dp0..\UDINet\bin\Release\Scs.dll %~dp0\%f%\Scs.dll

echo Build completed into '%f%' folder

:: Zipping
echo Zipping folder %f%..
del "%~dp0\%f%.zip" 2>nul
call zip "%~dp0\%f%" "%~dp0\%f%.zip"
echo Removing %f%..
rmdir /S /Q %~dp0\%f%
