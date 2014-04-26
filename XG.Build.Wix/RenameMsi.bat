@echo off

REM capture script output in variable
for /f "usebackq tokens=*" %%a in (`cscript "%~dp0\getmsiversion.vbs" "%~f1"`) do (set myvar=%%a)

SET MSINAME="%~dp1%~n1_%myvar%%~x1"
echo Renaming to %MSINAME%
move /Y %1 %MSINAME%

