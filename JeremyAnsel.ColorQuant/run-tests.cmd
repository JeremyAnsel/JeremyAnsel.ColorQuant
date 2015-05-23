@echo off
setlocal

cd "%~dp0"

if '%Configuration%' == '' if not '%1' == '' set Configuration=%1
if '%Configuration%' == '' set Configuration=Debug

if exist build\coverage rd /s /q build\coverage
md build\coverage

packages\OpenCover.4.5.3723\OpenCover.Console.exe -register:user -output:build\coverage\results.xml -target:packages\xunit.runner.console.2.0.0\tools\xunit.console.exe -targetargs:"JeremyAnsel.ColorQuant.Tests\bin\%Configuration%\JeremyAnsel.ColorQuant.Tests.dll -noshadow" "-filter:+[JeremyAnsel.ColorQuant]* -[*.Tests]*" -hideskipped:File;Filter;Attribute -returntargetcode
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

packages\ReportGenerator.2.1.4.0\ReportGenerator.exe -reports:build\coverage\results.xml -targetdir:build\coverage -verbosity:Info
