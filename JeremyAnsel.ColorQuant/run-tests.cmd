@echo off
setlocal

if '%Configuration%' == '' if not '%1' == '' set Configuration=%1
if '%Configuration%' == '' set Configuration=Debug

if exist "%~dp0\build\coverage" rd /s /q "%~dp0\build\coverage"
md "%~dp0\build\coverage"

"%~dp0\packages\OpenCover.4.5.3723\OpenCover.Console.exe" -register:user -output:"%~dp0\build\coverage\results.xml" -target:"%~dp0\packages\xunit.runner.console.2.0.0\tools\xunit.console.exe" -targetargs:"%~dp0\JeremyAnsel.ColorQuant.Tests\bin\%Configuration%\JeremyAnsel.ColorQuant.Tests.dll -noshadow" "-filter:+[JeremyAnsel.ColorQuant]* -[*.Tests]*" -hideskipped:File;Filter;Attribute -returntargetcode
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

"%~dp0\packages\ReportGenerator.2.1.4.0\ReportGenerator.exe" -reports:"%~dp0\build\coverage\results.xml" -targetdir:"%~dp0\build\coverage" -verbosity:Info
